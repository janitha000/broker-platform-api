namespace Notification.Application.Notifications.SendNotification;

public enum SendNotificationKind
{
    Sent,
    Failed,
    IdempotencyConflict,
    TemplateNotFound,
}

public sealed record SendNotificationOutcome(
    SendNotificationKind Kind,
    SendNotificationResult? Notification);