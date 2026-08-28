using Origination.Application.Abstractions;
using Origination.Domain.Cases;

namespace Origination.Application.Cases.GetCases;

public sealed class GetCasesHandler
{
    private readonly ICaseRepository _caseRepository;
    private readonly ICurrentBroker _currentBroker;

    public GetCasesHandler(ICaseRepository caseRepository, ICurrentBroker currentBroker)
    {
        _caseRepository = caseRepository;
        _currentBroker = currentBroker;
    }

    public async Task<GetCasesResult> Handle(GetCasesQuery query, CancellationToken cancellationToken = default)
    {
        var cases = await _caseRepository.GetCasesByTenantId(_currentBroker.TenantId, cancellationToken);
        return new GetCasesResult(cases.Select(c => new CaseDto(c.Id, c.Status, c.InquiryNotes)));
    }
}