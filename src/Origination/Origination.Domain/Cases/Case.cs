namespace Origination.Domain.Cases;

public class Case
{
    public Guid Id { get; set; }
    public Guid BrokerId { get; set; }
    public string InquiryNotes { get; set; } = string.Empty;
    public FactFind? FactFind { get; set; }
    public CaseStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}