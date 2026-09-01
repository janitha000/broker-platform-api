namespace Notification.Domain.Notifications;

public sealed class NotificationTemplate
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Locale { get; set; } = "en-AU";
    public int Version { get; set; }
    public string SubjectTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}