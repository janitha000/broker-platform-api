namespace Notification.Application.Notifications.SendNotification;

public enum SendNotificationKind
{
    Sent,
    Failed,
    IdempotencyConflict,
}

public sealed record SendNotificationOutcome(
    SendNotificationKind Kind,
    SendNotificationResult? Notification);
