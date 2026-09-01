namespace Origination.Domain.Outbox;

public static class OutboxMessageTypes
{
    public const string CaseFactFindCompleted = "CaseFactFindCompleted";
}

public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public DateTime? PublishedAt { get; set; }
}