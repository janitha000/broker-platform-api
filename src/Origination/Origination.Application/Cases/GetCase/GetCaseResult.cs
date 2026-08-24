namespace Origination.Application.Cases.GetCase;

using Origination.Domain.Cases;


public sealed record GetCaseResult(Guid caseId, CaseStatus Status);