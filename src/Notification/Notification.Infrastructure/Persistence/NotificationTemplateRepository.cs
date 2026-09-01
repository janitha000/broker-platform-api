using Microsoft.EntityFrameworkCore;
using Notification.Domain.Notifications;

namespace Notification.Infrastructure.Persistence;

public sealed class NotificationTemplateRepository : INotificationTemplateRepository
{
    private readonly NotificationDbContext _db;

    public NotificationTemplateRepository(NotificationDbContext db)
    {
        _db = db;
    }

    public Task<NotificationTemplate?> GetActive(
        string key,
        string channel,
        CancellationToken cancellationToken = default) =>
        _db.Templates
            .AsNoTracking()
            .Where(t => t.Key == key && t.Channel == channel && t.IsActive)
            .OrderByDescending(t => t.Version)
            .FirstOrDefaultAsync(cancellationToken);
}