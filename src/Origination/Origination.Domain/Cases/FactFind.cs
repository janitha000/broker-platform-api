namespace Origination.Domain.Cases;

public sealed class FactFind
{
    public string Objectives { get; set; } = string.Empty;
    public decimal Income { get; set; }
    public decimal Expenses { get; set; }
    public decimal Assets { get; set; }
    public decimal Debts { get; set; }
    public DateTime CompletedAt { get; set; }
}