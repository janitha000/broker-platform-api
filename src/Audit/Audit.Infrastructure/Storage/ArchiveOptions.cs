namespace Audit.Infrastructure.Storage;

public sealed class ArchiveOptions
{
    public const string SectionName = "Archive";
    public string AwsRegion { get; set; } = "ap-southeast-2";
    public string Bucket { get; set; } = string.Empty;
}
