
namespace Origination.Application.Cases.GetCases;

public sealed record CaseDto(Guid CaseId, CaseStatus Status, string InquiryNotes);