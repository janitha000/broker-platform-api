using System.Collections.Concurrent;
using Notification.Domain.Notifications;
using NotificationEntity = Notification.Domain.Notifications.Notification;

namespace Notification.Infrastructure.Persistence;

public sealed class InMemoryNotificationRepository : INotificationRepository
{
    private readonly ConcurrentDictionary<string, NotificationEntity> _byKey = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<Guid, NotificationEntity> _byId = new();

    public Task<NotificationEntity?> GetByIdempotencyKey(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        _byKey.TryGetValue(idempotencyKey, out var notification);
        return Task.FromResult(notification);
    }

    public Task<NotificationEntity?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        _byId.TryGetValue(id, out var notification);
        return Task.FromResult(notification);
    }

    public Task<NotificationEntity> Add(NotificationEntity notification, CancellationToken cancellationToken = default)
    {
        if (!_byKey.TryAdd(notification.IdempotencyKey, notification))
            throw new InvalidOperationException("Idempotency key already stored.");
        _byId[notification.Id] = notification;
        return Task.FromResult(notification);
    }
}
