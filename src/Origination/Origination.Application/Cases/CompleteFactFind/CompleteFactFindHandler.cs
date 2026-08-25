using Origination.Domain.Cases;
using Origination.Application.Abstractions;
namespace Origination.Application.Cases.CompleteFactFind;

public sealed class CompleteFactFindHandler
{
    private readonly ICaseRepository _caseRepository;
    private readonly ICurrentBroker _currentBroker;

    public CompleteFactFindHandler(ICaseRepository caseRepository, ICurrentBroker currentBroker)
    {
        _caseRepository = caseRepository;
        _currentBroker = currentBroker;
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

        return new CompleteFactFindResult(@case.Id, @case.Status);
    }
}