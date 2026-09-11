using Origination.Application.Abstractions;
using Origination.Domain.Cases;

namespace Origination.Application.Cases.GetCases;

public sealed record GetCasesForBoardQuery();
public sealed record GetCasesForBoardResult(IEnumerable<CaseBoardDto> Cases);

public sealed class GetCasesForBoardHandler
{
    private readonly ICaseRepository _caseRepository;
    private readonly ICurrentBroker _currentBroker;

    public GetCasesForBoardHandler(ICaseRepository caseRepository, ICurrentBroker currentBroker)
    {
        _caseRepository = caseRepository;
        _currentBroker = currentBroker;
    }

    public async Task<GetCasesForBoardResult> Handle(GetCasesForBoardQuery query, CancellationToken cancellationToken = default)
    {
        var cases = await _caseRepository.GetCasesByTenantId(_currentBroker.TenantId, cancellationToken);
        return new GetCasesForBoardResult(cases.Select(c => new CaseBoardDto(c.Id, c.BrokerId, c.InquiryNotes, c.FactFind, c.Status, c.CreatedAt)));
    }
}