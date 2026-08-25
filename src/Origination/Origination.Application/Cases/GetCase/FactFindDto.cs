namespace Origination.Application.Cases.GetCase;

public sealed record FactFindDto(
    string Objectives,
    decimal Income,
    decimal Expenses,
    decimal Assets,
    decimal Debts,
    DateTime CompletedAt);