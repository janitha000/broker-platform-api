namespace Notification.Application.Notifications.SendNotification;

public sealed record SendNotificationCommand(
    string Channel,
    string Recipient,
    string Subject,
    string Body,
    string Source,
    string IdempotencyKey,
    string? CorrelationId);
