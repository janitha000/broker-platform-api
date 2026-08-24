using Origination.Domain.Cases;

namespace Origination.Application.Cases.CreateCase;

public sealed record CreateCaseResult(Guid CaseId, CaseStatus Status);
