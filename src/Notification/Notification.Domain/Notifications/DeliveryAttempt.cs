namespace Notification.Domain.Notifications;

public sealed class DeliveryAttempt
{
    public Guid Id { get; set; }
    public Guid NotificationId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool Succeeded { get; set; }
    public string? ProviderMessageId { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}