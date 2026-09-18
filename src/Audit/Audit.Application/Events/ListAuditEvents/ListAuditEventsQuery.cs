namespace Audit.Application.Events.ListAuditEvents;

public sealed record ListAuditEventsQuery(
    DateTime? FromUtc,
    DateTime? ToUtc,
    Guid? CaseId,
    string? Action,
    string? Outcome,
    int Take);

public sealed record AuditEventItem(
    Guid EventId,
    DateTime OccurredAt,
    string Action,
    string Outcome,
    string ActorType,
    Guid? BrokerId,
    string ResourceType,
    string ResourceId,
    Guid? CaseId,
    string? Sensitivity,
    string? CorrelationId,
    string? DataJson,
    string RecordHash);

public sealed record ListAuditEventsResult(IReadOnlyList<AuditEventItem> Items);
