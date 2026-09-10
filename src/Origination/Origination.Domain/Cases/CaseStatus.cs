namespace Origination.Domain.Cases;

/// <summary>
/// Australian mortgage origination pipeline (enquiry through settlement).
/// Fact-find is form-gated; later moves are board/command edges.
/// </summary>
public enum CaseStatus
{
    Enquiry,
    FactFindCompleted,
    Recommendation,
    Lodged,
    ConditionalApproval,
    FormalApproval,
    Settled,
    NotProceeded,
}
