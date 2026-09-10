namespace Notification.Application.Abstractions;

public sealed record RealtimeNotification(
    string Type,
    Guid TenantId,
    Guid? CaseId,
    Guid? NotificationId);

public interface IRealtimeNotifier
{
    Task Publish(RealtimeNotification message, CancellationToken cancellationToken = default);
}
