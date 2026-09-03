using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Notification.Domain.Inbox;

namespace Notification.Infrastructure.Persistence;

public sealed class Inbox : IInbox
{
    private readonly NotificationDbContext _context;

    public Inbox(NotificationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> TryAdd(InboxMessage message, CancellationToken cancellationToken = default)
    {
        _context.InboxMessages.Add(message);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            foreach (var entry in _context.ChangeTracker.Entries().ToList())
                entry.State = EntityState.Detached;
            return false;
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException sql && sql.Number is 2601 or 2627;
}