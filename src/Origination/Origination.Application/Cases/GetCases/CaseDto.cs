using Origination.Domain.Cases;

namespace Origination.Application.Cases.GetCases;

public sealed record CaseDto(Guid CaseId, CaseStatus Status, string InquiryNotes);
public sealed record CaseBoardDto(Guid CaseId, Guid BrokerId, string InquiryNotes, FactFind? FactFind, CaseStatus Status, DateTime CreatedAt );

