using System.Text.Json;
using Origination.Application.Abstractions;
using Origination.Domain.Abstractions;
using Origination.Domain.Cases;
using Origination.Domain.Outbox;

namespace Origination.Application.Cases.CompleteFactFind;

public sealed class CompleteFactFindHandler
{
    private readonly ICaseRepository _caseRepository;
    private readonly ICurrentBroker _currentBroker;
    private readonly IOutbox _outbox;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteFactFindHandler(
        ICaseRepository caseRepository,
        ICurrentBroker currentBroker,
        IOutbox outbox,
        IUnitOfWork unitOfWork)
    {
        _caseRepository = caseRepository;
        _currentBroker = currentBroker;
        _outbox = outbox;
        _unitOfWork = unitOfWork;
    }

    public async Task<CompleteFactFindResult?> Handle(
        CompleteFactFindCommand command,
        CancellationToken cancellationToken = default)
    {
        var @case = await _caseRepository.GetById(command.CaseId, _currentBroker.TenantId, cancellationToken);
        if (@case is null)
            return null;

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
            _outbox.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = OutboxMessageTypes.CaseFactFindCompleted,
                IdempotencyKey = idempotencyKey,
                OccurredAt = DateTime.UtcNow,
                Payload = JsonSerializer.Serialize(new
                {
                    caseId = @case.Id,
                    tenantId = @case.TenantId,
                    brokerId = @case.BrokerId,
                    templateKey = "case.fact-find-completed",
                    channel = "Email",
                    data = new Dictionary<string, string>
                    {
                        ["caseId"] = @case.Id.ToString(),
                    },
                    idempotencyKey,
                    correlationId = @case.Id.ToString(),
                }),
            });
        }

        await _unitOfWork.SaveChanges(cancellationToken);
        return new CompleteFactFindResult(@case.Id, @case.Status);
    }
}
