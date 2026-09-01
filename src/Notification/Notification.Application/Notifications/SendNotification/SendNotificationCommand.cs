namespace Notification.Application.Notifications.SendNotification;

public sealed record SendNotificationCommand(
    string Channel,
    string Recipient,
    string TemplateKey,
    Dictionary<string, string>? Data,
    string Source,
    string IdempotencyKey,
    string? CorrelationId);