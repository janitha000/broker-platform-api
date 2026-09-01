using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Notification.Domain.Notifications;
using NotificationEntity = Notification.Domain.Notifications.Notification;

namespace Notification.Infrastructure.Persistence;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _db;

    public NotificationRepository(NotificationDbContext db)
    {
        _db = db;
    }

    public Task<NotificationEntity?> GetByIdempotencyKey(
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        _db.Notifications
            .Include(n => n.Attempts)
            .FirstOrDefaultAsync(n => n.IdempotencyKey == idempotencyKey, cancellationToken);

    public Task<NotificationEntity?> GetById(Guid id, CancellationToken cancellationToken = default) =>
        _db.Notifications
            .AsNoTracking()
            .Include(n => n.Attempts)
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public async Task<NotificationEntity> Add(NotificationEntity notification, CancellationToken cancellationToken = default)
    {
        _db.Notifications.Add(notification);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return notification;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            foreach (var entry in _db.ChangeTracker.Entries().ToList())
                entry.State = EntityState.Detached;

            var existing = await GetByIdempotencyKey(notification.IdempotencyKey, cancellationToken);
            if (existing is null)
                throw;
            return existing;
        }
    }

    public async Task Update(NotificationEntity notification, CancellationToken cancellationToken = default)
    {
        _db.Notifications.Update(notification);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException sql && sql.Number is 2601 or 2627;
}