namespace Notification.Application.Abstractions;

public sealed record EmailMessage(string To, string Subject, string Body);

public sealed record EmailSendResult(
    bool Succeeded,
    string? ProviderMessageId,
    string? ErrorCode,
    string? ErrorMessage);

public interface IEmailProvider
{
    Task<EmailSendResult> Send(EmailMessage message, CancellationToken cancellationToken = default);
}