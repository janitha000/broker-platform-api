namespace Broker.Hosting.Audit;

public sealed class AuditEvent
{
    public Guid EventId { get; set; }
    public DateTime OccurredAt { get; set; }
    public Guid TenantId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Outcome { get; set; } = AuditOutcomes.Allow;
    public AuditActor Actor { get; set; } = new();
    public AuditResource Resource { get; set; } = new();
    public string? CorrelationId { get; set; }
    public string? RequestId { get; set; }
    public string? TraceId { get; set; }
    public string? DataJson { get; set; }
}