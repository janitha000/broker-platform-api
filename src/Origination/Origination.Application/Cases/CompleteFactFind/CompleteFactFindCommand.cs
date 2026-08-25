namespace Origination.Application.Cases.CompleteFactFind;

public sealed record CompleteFactFindCommand(
    Guid CaseId,
    string Objectives,
    decimal Income,
    decimal Expenses,
    decimal Assets,
    decimal Debts);