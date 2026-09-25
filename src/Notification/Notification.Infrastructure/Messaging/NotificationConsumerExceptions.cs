namespace Notification.Infrastructure.Messaging;

public sealed class EmailDeliveryFailedException : Exception
{
    public EmailDeliveryFailedException()
        : base("Email send failed")
    {
    }
}

public sealed class NotificationTemplateNotFoundException : Exception
{
    public NotificationTemplateNotFoundException(string templateKey)
        : base($"Unknown template {templateKey}")
    {
    }
}
