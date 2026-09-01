using Notification.Application.Abstractions;

namespace Notification.Infrastructure.Senders;

public sealed class MockNotificationSender : INotificationSender
{
    public const string FailRecipient = "fail@example.com";

    public Task<bool> Send(
        string channel,
        string recipient,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        var ok = !string.Equals(recipient?.Trim(), FailRecipient, StringComparison.OrdinalIgnoreCase);
        return Task.FromResult(ok);
    }
}
