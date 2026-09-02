namespace Notification.Domain.Inbox;

public interface IInbox
{
    /// <returns>true if this process inserted the row; false if the key already existed.</returns>
    Task<bool> TryAdd(InboxMessage message, CancellationToken cancellationToken = default);
}