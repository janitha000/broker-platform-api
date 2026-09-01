using Notification.Application.Abstractions;
using Notification.Domain.Notifications;
using NotificationEntity = Notification.Domain.Notifications.Notification;

namespace Notification.Application.Notifications.SendNotification;

public sealed class SendNotificationHandler
{
    private readonly INotificationRepository _notifications;
    private readonly INotificationTemplateRepository _templates;
    private readonly ITemplateRenderer _renderer;
    private readonly IEmailProvider _email;

    public SendNotificationHandler(
        INotificationRepository notifications,
        INotificationTemplateRepository templates,
        ITemplateRenderer renderer,
        IEmailProvider email)
    {
        _notifications = notifications;
        _templates = templates;
        _renderer = renderer;
        _email = email;
    }

    public async Task<SendNotificationOutcome> Handle(
        SendNotificationCommand command,
        CancellationToken cancellationToken = default)
    {
        var key = command.IdempotencyKey.Trim();
        var channel = command.Channel.Trim();
        var recipient = command.Recipient.Trim();
        var templateKey = command.TemplateKey.Trim();
        var source = command.Source.Trim();
        var data = command.Data ?? new Dictionary<string, string>();
        var fingerprint = Fingerprint(channel, recipient, templateKey, data, source, command.CorrelationId);

        var existing = await _notifications.GetByIdempotencyKey(key, cancellationToken);
        if (existing is not null)
            return await ReplayConflictOrResume(existing, fingerprint, cancellationToken);

        var template = await _templates.GetActive(templateKey, channel, cancellationToken);
        if (template is null)
            return new SendNotificationOutcome(SendNotificationKind.TemplateNotFound, null);

        var subject = _renderer.Render(template.SubjectTemplate, data);
        var body = _renderer.Render(template.BodyTemplate, data);
        var now = DateTime.UtcNow;
        var notificationId = Guid.NewGuid();

        var notification = new NotificationEntity
        {
            Id = notificationId,
            Channel = channel,
            Recipient = recipient,
            TemplateKey = templateKey,
            TemplateData = SerializeData(data),
            RenderedSubject = subject,
            RenderedBody = body,
            Source = source,
            CorrelationId = string.IsNullOrWhiteSpace(command.CorrelationId) ? null : command.CorrelationId.Trim(),
            Status = NotificationStatus.Accepted,
            IdempotencyKey = key,
            PayloadFingerprint = fingerprint,
            CreatedAt = now,
        };

        notification.Attempts.Add(new DeliveryAttempt
        {
            Id = Guid.NewGuid(),
            NotificationId = notificationId,
            StartedAt = now,
        });

        notification = await _notifications.Add(notification, cancellationToken);
        return await ReplayConflictOrResume(notification, fingerprint, cancellationToken);
    }

    private async Task<SendNotificationOutcome> ReplayConflictOrResume(
        NotificationEntity notification,
        string fingerprint,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(notification.PayloadFingerprint, fingerprint, StringComparison.Ordinal))
            return new SendNotificationOutcome(SendNotificationKind.IdempotencyConflict, null);

        if (notification.Status is NotificationStatus.Sent or NotificationStatus.Failed)
            return ToOutcome(notification);

        return await CompleteSend(notification, cancellationToken);
    }

    private async Task<SendNotificationOutcome> CompleteSend(
        NotificationEntity notification,
        CancellationToken cancellationToken)
    {
        var attempt = notification.Attempts.FirstOrDefault(a => a.CompletedAt is null);
        if (attempt is null)
        {
            attempt = new DeliveryAttempt
            {
                Id = Guid.NewGuid(),
                NotificationId = notification.Id,
                StartedAt = DateTime.UtcNow,
            };
            notification.Attempts.Add(attempt);
        }

        var send = await _email.Send(
            new EmailMessage(notification.Recipient, notification.RenderedSubject, notification.RenderedBody),
            cancellationToken);

        attempt.CompletedAt = DateTime.UtcNow;
        attempt.Succeeded = send.Succeeded;
        attempt.ProviderMessageId = send.ProviderMessageId;
        attempt.ErrorCode = send.ErrorCode;
        attempt.ErrorMessage = send.ErrorMessage;

        notification.Status = send.Succeeded ? NotificationStatus.Sent : NotificationStatus.Failed;
        notification.SentAt = send.Succeeded ? attempt.CompletedAt : null;
        await _notifications.Update(notification, cancellationToken);
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
        string templateKey,
        IReadOnlyDictionary<string, string> data,
        string source,
        string? correlationId)
    {
        var dataPart = string.Join("&", data.OrderBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => p.Key + "=" + p.Value));
        return $"{channel}|{recipient}|{templateKey}|{dataPart}|{source}|{correlationId?.Trim()}";
    }

    private static string SerializeData(IReadOnlyDictionary<string, string> data) =>
        string.Join("&", data.OrderBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => p.Key + "=" + p.Value));
}