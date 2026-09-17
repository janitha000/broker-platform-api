namespace Broker.Hosting.Audit;

public static class AuditResourceTypes
{
    public const string Case = "case";
    public const string Document = "document";
    public const string Tenant = "tenant";
    public const string User = "user";
    public const string Endpoint = "endpoint";
}

public sealed class AuditResource
{
    public string Type { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public Guid? CaseId { get; set; }
    public string? Sensitivity { get; set; }
}