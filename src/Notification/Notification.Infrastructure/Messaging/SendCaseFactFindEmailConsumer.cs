using Broker.Contracts.Notification;
using MassTransit;
using Notification.Application.Notifications.SendNotification;

namespace Notification.Infrastructure.Messaging;

public sealed class SendCaseFactFindEmailConsumer : IConsumer<SendCaseFactFindEmail>
{
    private readonly SendNotificationHandler _handler;

    public SendCaseFactFindEmailConsumer(SendNotificationHandler handler)
    {
        _handler = handler;
    }

    public async Task Consume(ConsumeContext<SendCaseFactFindEmail> context)
    {
        var message = context.Message;
        var outcome = await _handler.Handle(
            new SendNotificationCommand(
                string.IsNullOrWhiteSpace(message.Channel) ? "Email" : message.Channel,
                message.Recipient ?? $"broker-{message.BrokerId}@invalid.local",
                message.TemplateKey,
                message.Data,
                "origination",
                message.IdempotencyKey,
                message.CorrelationId,
                message.TenantId,
                message.CaseId),
            context.CancellationToken);

        if (outcome.Kind is SendNotificationKind.TemplateNotFound)
            throw new NotificationTemplateNotFoundException(message.TemplateKey);

        if (outcome.Kind is SendNotificationKind.Failed)
            throw new EmailDeliveryFailedException();
    }
}
