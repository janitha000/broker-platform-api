namespace Notification.Application.Abstractions;

public interface INotificationSender
{
    Task<bool> Send(
        string channel,
        string recipient,
        string subject,
        string body,
        CancellationToken cancellationToken = default);
}
