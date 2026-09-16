using Origination.Domain.Cases;

namespace Origination.Application.Cases.CompleteFactFind;

public enum CompleteFactFindKind
{
    Succeeded,
    NotFound,
    Forbidden,
}

public sealed record CompleteFactFindResult(Guid CaseId, CaseStatus Status);

public sealed record CompleteFactFindOutcome(
    CompleteFactFindKind Kind,
    CompleteFactFindResult? Result);