using Broker.Hosting.Audit;
using Origination.Application.Abstractions;
using Origination.Domain.Abstractions;
using Origination.Domain.Cases;

namespace Origination.Application.Cases.GetCase;

public sealed class GetCaseHandler
{
    private readonly ICaseRepository _caseRepository;
    private readonly ICurrentBroker _currentBroker;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditRecorder _audit;

    public GetCaseHandler(
        ICaseRepository caseRepository,
        ICurrentBroker currentBroker,
        IUnitOfWork unitOfWork,
        IAuditRecorder audit)
    {
        _caseRepository = caseRepository;
        _currentBroker = currentBroker;
        _unitOfWork = unitOfWork;
        _audit = audit;
    }

    public async Task<GetCaseResult?> Handle(GetCaseQuery query, CancellationToken cancellationToken = default)
    {
        var @case = await _caseRepository.GetById(query.CaseId, _currentBroker.TenantId, cancellationToken);
        if (@case is null)
            return null;

        _audit.Record(new AuditEvent
        {
            EventId = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
            TenantId = @case.TenantId,
            Action = AuditActions.CaseView,
            Outcome = AuditOutcomes.Allow,
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
        await _unitOfWork.SaveChanges(cancellationToken);

        FactFindDto? factFind = @case.FactFind is null
            ? null
            : new FactFindDto(
                @case.FactFind.Objectives,
                @case.FactFind.Income,
                @case.FactFind.Expenses,
                @case.FactFind.Assets,
                @case.FactFind.Debts,
                @case.FactFind.CompletedAt);

        return new GetCaseResult(@case.Id, @case.Status, @case.InquiryNotes, factFind);
    }
}
