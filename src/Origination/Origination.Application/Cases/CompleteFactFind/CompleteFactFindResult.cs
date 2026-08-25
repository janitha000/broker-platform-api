using Origination.Domain.Cases;

namespace Origination.Application.Cases.CompleteFactFind;

public sealed record CompleteFactFindResult(Guid CaseId, CaseStatus Status);
