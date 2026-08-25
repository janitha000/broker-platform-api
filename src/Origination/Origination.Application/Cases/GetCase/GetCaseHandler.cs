using Origination.Domain.Cases;

namespace Origination.Application.Cases.GetCase;

public sealed class GetCaseHandler
{
    private readonly ICaseRepository _caseRepository;

    public GetCaseHandler(ICaseRepository caseRepository)
    {
        _caseRepository = caseRepository;
    }

    public async Task<GetCaseResult?> Handle(GetCaseQuery query, CancellationToken cancellationToken = default)
    {
        var @case = await _caseRepository.GetById(query.CaseId, cancellationToken);
        if (@case is null) 
            return null;

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