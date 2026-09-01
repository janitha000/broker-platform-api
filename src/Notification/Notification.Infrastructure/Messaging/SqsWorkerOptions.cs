namespace Notification.Infrastructure.Messaging;

public sealed class SqsWorkerOptions
{
    public const string SectionName = "Messaging";
    public string QueueUrl { get; set; } = string.Empty;
    public string AwsRegion { get; set; } = "ap-southeast-2";
}