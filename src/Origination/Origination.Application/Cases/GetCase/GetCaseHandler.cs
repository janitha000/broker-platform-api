namespace Origination.Application.Cases.GetCase;

using Origination.Domain.Cases;

public sealed class GetCaseHandler
{
    private readonly ICaseRepository _caseRepository;

    public GetCaseHandler(ICaseRepository caseRepository)
    {
        _caseRepository = caseRepository;
    }

    public async Task<GetCaseResult> Handle(GetCaseQuery query, CancellationToken cancellationToken = default)
    {
        var @case = await _caseRepository.GetById(query.CaseId, cancellationToken);
        if (@case is null) 
            return null;
        return new GetCaseResult(@case.Id, @case.Status);
    }
}