namespace Notification.Domain.Inbox;

public static class InboxStatus
{
    public const string Received = "Received";
    public const string Processed = "Processed";
    public const string Dead = "Dead";
}

public sealed class InboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string Status { get; set; } = InboxStatus.Received;
    public DateTime ReceivedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime NextAttemptAt { get; set; }
    public DateTime? LockedUntil { get; set; }
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
}