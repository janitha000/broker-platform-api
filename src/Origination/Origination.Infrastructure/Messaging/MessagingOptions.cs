namespace Origination.Infrastructure.Messaging;

public sealed class MessagingOptions
{
    public const string SectionName = "Messaging";
    public string Provider { get; set; } = "Logging";
    public string EventBusName { get; set; } = "broker-platform";
    public string Source { get; set; } = "origination.broker-platform";
    public string AwsRegion { get; set; } = "ap-southeast-2";
}