using Notification.Application.Abstractions;

namespace Notification.Infrastructure.Email;

public sealed class MockEmailProvider : IEmailProvider
{
    public const string FailRecipient = "fail@example.com";

    public Task<EmailSendResult> Send(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (string.Equals(message.To.Trim(), FailRecipient, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new EmailSendResult(
                false, null, "MockDecline", "Mock provider declined recipient"));
        }

        return Task.FromResult(new EmailSendResult(
            true, Guid.NewGuid().ToString("N"), null, null));
    }
}