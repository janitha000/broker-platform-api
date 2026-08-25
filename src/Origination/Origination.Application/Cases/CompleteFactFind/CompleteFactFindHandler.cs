using Origination.Domain.Cases;

namespace Origination.Application.Cases.CompleteFactFind;

public sealed class CompleteFactFindHandler
{
    private readonly ICaseRepository _caseRepository;

    public CompleteFactFindHandler(ICaseRepository caseRepository)
    {
        _caseRepository = caseRepository;
    }

    public async Task<CompleteFactFindResult?> Handle(
        CompleteFactFindCommand command,
        CancellationToken cancellationToken = default)    
    {
        var @case = await _caseRepository.GetById(command.CaseId, cancellationToken);
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