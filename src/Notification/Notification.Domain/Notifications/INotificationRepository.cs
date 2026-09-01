namespace Notification.Domain.Notifications;

public interface INotificationRepository
{
    Task<Notification?> GetByIdempotencyKey(string idempotencyKey, CancellationToken cancellationToken = default);
    Task<Notification?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<Notification> Add(Notification notification, CancellationToken cancellationToken = default);
}
