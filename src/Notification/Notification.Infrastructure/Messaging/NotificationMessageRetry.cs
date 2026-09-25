using MassTransit;

namespace Notification.Infrastructure.Messaging;

public static class NotificationMessageRetry
{
    public const int EmailImmediateRetries = 3;

    public static void Configure(IRetryConfigurator retry)
    {
        retry.Handle<EmailDeliveryFailedException>();
        retry.Ignore<NotificationTemplateNotFoundException>();
        retry.Immediate(EmailImmediateRetries);
    }
}
