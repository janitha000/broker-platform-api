namespace Notification.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";
    public string Provider { get; set; } = "Mock";
    public string FromAddress { get; set; } = "noreply@localhost";
    public string AwsRegion { get; set; } = "ap-southeast-2";
}