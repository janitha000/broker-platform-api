namespace Notification.Domain.Notifications;

public static class NotificationChannel
{
    public const string Email = "Email";
}

public static class NotificationStatus
{
    public const string Accepted = "Accepted";
    public const string Sent = "Sent";
    public const string Failed = "Failed";
}

public sealed class Notification
{
    public Guid Id { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public string TemplateKey { get; set; } = string.Empty;
    public string TemplateData { get; set; } = "{}";
    public string RenderedSubject { get; set; } = string.Empty;
    public string RenderedBody { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string PayloadFingerprint { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public ICollection<DeliveryAttempt> Attempts { get; set; } = new List<DeliveryAttempt>();
}