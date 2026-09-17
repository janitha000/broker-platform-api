namespace Document.Infrastructure.Storage;

public sealed class S3StorageOptions
{
    public const string SectionName = "Storage";
    public string AwsRegion { get; set; } = "ap-southeast-2";
    public string LandingBucket { get; set; } = string.Empty;
    public string CleanBucket { get; set; } = string.Empty;
    public string QuarantineBucket { get; set; } = string.Empty;
}