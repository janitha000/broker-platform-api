namespace Broker.Hosting.Audit;

public static class AuditActorTypes
{
    public const string User = "user";
    public const string System = "system";
    public const string Worker = "worker";
}

public sealed class AuditActor
{
    public string Type { get; set; } = AuditActorTypes.User;
    public Guid? BrokerId { get; set; }
    public string? Subject { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}