using Broker.Contracts.Origination;
using Broker.Hosting.Audit;
using Origination.Application.Abstractions;
using Origination.Application.Auth;
using Origination.Domain.Abstractions;
using Origination.Domain.Cases;
using Origination.Domain.Outbox;
using System.Text.Json;

namespace Origination.Application.Cases.CompleteFactFind;

public sealed class CompleteFactFindHandler
{
    private readonly ICaseRepository _caseRepository;
    private readonly ICurrentBroker _currentBroker;
    private readonly IOutbox _outbox;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditRecorder _audit;
    private readonly ICaseFactFindCompletedPublisher _factFindCompleted;

    public CompleteFactFindHandler(
        ICaseRepository caseRepository,
        ICurrentBroker currentBroker,
        IOutbox outbox,
        IUnitOfWork unitOfWork,
        IAuditRecorder audit,
        ICaseFactFindCompletedPublisher factFindCompleted)
    {
        _caseRepository = caseRepository;
        _currentBroker = currentBroker;
        _outbox = outbox;
        _unitOfWork = unitOfWork;
        _audit = audit;
        _factFindCompleted = factFindCompleted;
    }

    public async Task<CompleteFactFindOutcome> Handle(
        CompleteFactFindCommand command,
        CancellationToken cancellationToken = default)
    {
        var @case = await _caseRepository.GetById(command.CaseId, _currentBroker.TenantId, cancellationToken);
        if (@case is null)
            return new CompleteFactFindOutcome(CompleteFactFindKind.NotFound, null);

        var canAny = _currentBroker.HasPermission(CasePermissions.FactFindAny);
        var canOwn = _currentBroker.HasPermission(CasePermissions.FactFind)
            && @case.BrokerId == _currentBroker.BrokerId;
        if (!canAny && !canOwn)
        {
            RecordCaseAudit(@case, AuditActions.CaseFactFindComplete, AuditOutcomes.Deny);
            await _unitOfWork.SaveChanges(cancellationToken);
            return new CompleteFactFindOutcome(CompleteFactFindKind.Forbidden, null);
        }

        @case.FactFind = new FactFind
        {
            Objectives = command.Objectives,
            Income = command.Income,
            Expenses = command.Expenses,
            Assets = command.Assets,
            Debts = command.Debts,
            CompletedAt = DateTime.UtcNow,
        };

        @case.Status = CaseStatus.FactFindCompleted;
        await _caseRepository.Update(@case, cancellationToken);

        var idempotencyKey = $"origination:{@case.Id}:fact-find-completed:email";
        if (!await _outbox.Exists(idempotencyKey, cancellationToken))
        {
            var message = new CaseFactFindCompleted
            {
                CaseId = @case.Id,
                TenantId = @case.TenantId,
                BrokerId = @case.BrokerId,
                TemplateKey = "case.fact-find-completed",
                Channel = "Email",
                Data = new Dictionary<string, string> { ["caseId"] = @case.Id.ToString() },
                IdempotencyKey = idempotencyKey,
                CorrelationId = @case.Id.ToString(),
            };

            if (_factFindCompleted.UsesBusOutbox)
                await _factFindCompleted.Publish(message, cancellationToken);

            _outbox.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = OutboxMessageTypes.CaseFactFindCompleted,
                IdempotencyKey = idempotencyKey,
                OccurredAt = DateTime.UtcNow,
                PublishedAt = _factFindCompleted.UsesBusOutbox ? DateTime.UtcNow : null,
                Payload = _factFindCompleted.UsesBusOutbox
                    ? "{}"
                    : JsonSerializer.Serialize(new
                    {
                        caseId = message.CaseId,
                        tenantId = message.TenantId,
                        brokerId = message.BrokerId,
                        templateKey = message.TemplateKey,
                        channel = message.Channel,
                        data = message.Data,
                        idempotencyKey,
                        correlationId = message.CorrelationId,
                    }),
            });
        }

        RecordCaseAudit(@case, AuditActions.CaseFactFindComplete, AuditOutcomes.Allow);
        await _unitOfWork.SaveChanges(cancellationToken);
        return new CompleteFactFindOutcome(
            CompleteFactFindKind.Succeeded,
            new CompleteFactFindResult(@case.Id, @case.Status));
    }

    private void RecordCaseAudit(Case @case, string action, string outcome) =>
        _audit.Record(new AuditEvent
        {
            EventId = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
            TenantId = @case.TenantId,
            Action = action,
            Outcome = outcome,
            Actor = new AuditActor
            {
                Type = AuditActorTypes.User,
                BrokerId = _currentBroker.BrokerId,
            },
            Resource = new AuditResource
            {
                Type = AuditResourceTypes.Case,
                Id = @case.Id.ToString("D"),
                CaseId = @case.Id,
            },
        });
}
