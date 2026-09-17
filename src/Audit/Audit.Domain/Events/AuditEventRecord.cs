namespace Audit.Domain.Events;

public sealed class AuditEventRecord
{
    public Guid EventId { get; set; }
    public DateTime OccurredAt { get; set; }
    public DateTime IngestedAt { get; set; }
    public Guid TenantId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public string ActorType { get; set; } = string.Empty;
    public Guid? BrokerId { get; set; }
    public string? Subject { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public Guid? CaseId { get; set; }
    public string? Sensitivity { get; set; }
    public string? CorrelationId { get; set; }
    public string? RequestId { get; set; }
    public string? DataJson { get; set; }
    public string EnvelopeJson { get; set; } = string.Empty;
    public string? PrevHash { get; set; }
    public string RecordHash { get; set; } = string.Empty;
    public DateTime? ArchivedAt { get; set; }
}

public sealed class TenantAuditHead
{
    public Guid TenantId { get; set; }
    public Guid LastEventId { get; set; }
    public string LastHash { get; set; } = string.Empty;
}