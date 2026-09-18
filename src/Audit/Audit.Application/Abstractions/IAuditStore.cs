using Audit.Domain.Events;

namespace Audit.Application.Abstractions;

public interface ICurrentBroker
{
    Guid BrokerId { get; }
    Guid TenantId { get; }
    bool HasPermission(string permission);
}

public interface IAuditStore
{
    Task<AuditEventRecord?> GetByEventId(Guid eventId, CancellationToken ct);
    Task<AuditEventRecord> InsertChained(AuditEventRecord record, CancellationToken ct);
    Task MarkArchived(Guid eventId, CancellationToken ct);
    Task<IReadOnlyList<AuditEventRecord>> List(
        Guid tenantId,
        DateTime? fromUtc,
        DateTime? toUtc,
        Guid? caseId,
        string? action,
        string? outcome,
        int take,
        CancellationToken ct);
}

public interface IAuditArchive
{
    Task Put(AuditEventRecord record, CancellationToken ct);
}
