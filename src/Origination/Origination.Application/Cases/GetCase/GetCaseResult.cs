using Origination.Domain.Cases;

namespace Origination.Application.Cases.GetCase;

public sealed record GetCaseResult(Guid CaseId, CaseStatus Status, string InquiryNotes, FactFindDto? FactFind);
