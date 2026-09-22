namespace Broker.Hosting.RateLimiting;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public bool Enabled { get; set; } = true;

    /// <summary>Authenticated (or other) API calls per partition per window.</summary>
    public int PermitLimit { get; set; } = 120;

    public int WindowSeconds { get; set; } = 60;

    /// <summary>Anonymous /auth* by IP — tighter against brute force.</summary>
    public int AuthPermitLimit { get; set; } = 20;

    public int AuthWindowSeconds { get; set; } = 60;
}
