using Notification.Application.Abstractions;
using Notification.Domain.Notifications;
using NotificationEntity = Notification.Domain.Notifications.Notification;

namespace Notification.Application.Notifications.SendNotification;

public sealed class SendNotificationHandler
{
    private readonly INotificationRepository _notifications;
    private readonly INotificationSender _sender;

    public SendNotificationHandler(
        INotificationRepository notifications,
        INotificationSender sender)
    {
        _notifications = notifications;
        _sender = sender;
    }

    public async Task<SendNotificationOutcome> Handle(
        SendNotificationCommand command,
        CancellationToken cancellationToken = default)
    {
        var key = command.IdempotencyKey.Trim();
        var channel = command.Channel.Trim();
        var recipient = command.Recipient.Trim();
        var subject = command.Subject.Trim();
        var body = command.Body;
        var source = command.Source.Trim();
        var fingerprint = Fingerprint(channel, recipient, subject, body, source, command.CorrelationId);

        var existing = await _notifications.GetByIdempotencyKey(key, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.PayloadFingerprint, fingerprint, StringComparison.Ordinal))
                return new SendNotificationOutcome(SendNotificationKind.IdempotencyConflict, null);

            return ToOutcome(existing);
        }

        var sent = await _sender.Send(channel, recipient, subject, body, cancellationToken);
        var now = DateTime.UtcNow;
        var notification = new NotificationEntity
        {
            Id = Guid.NewGuid(),
            Channel = channel,
            Recipient = recipient,
            Subject = subject,
            Body = body,
            Source = source,
            CorrelationId = string.IsNullOrWhiteSpace(command.CorrelationId)
                ? null
                : command.CorrelationId.Trim(),
            Status = sent ? NotificationStatus.Sent : NotificationStatus.Failed,
            IdempotencyKey = key,
            PayloadFingerprint = fingerprint,
            CreatedAt = now,
            SentAt = sent ? now : null,
        };
        notification = await _notifications.Add(notification, cancellationToken);
        return ToOutcome(notification);
    }

    private static SendNotificationOutcome ToOutcome(NotificationEntity notification)
    {
        var result = new SendNotificationResult(notification.Id, notification.Status);
        var kind = notification.Status == NotificationStatus.Failed
            ? SendNotificationKind.Failed
            : SendNotificationKind.Sent;
        return new SendNotificationOutcome(kind, result);
    }

    private static string Fingerprint(
        string channel,
        string recipient,
        string subject,
        string body,
        string source,
        string? correlationId) =>
        $"{channel}|{recipient}|{subject}|{body}|{source}|{correlationId?.Trim()}";
}
