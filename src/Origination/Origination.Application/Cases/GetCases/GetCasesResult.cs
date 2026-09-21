using Origination.Domain.Cases;

namespace Origination.Application.Cases.GetCases;

public sealed record GetCasesResult(IReadOnlyList<CaseDto> Cases);
