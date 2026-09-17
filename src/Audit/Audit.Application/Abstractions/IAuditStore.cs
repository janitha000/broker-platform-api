using Audit.Domain.Events;

namespace Audit.Application.Abstractions;

public interface IAuditStore
{
    Task<AuditEventRecord?> GetByEventId(Guid eventId, CancellationToken ct);
    Task<AuditEventRecord> InsertChained(AuditEventRecord record, CancellationToken ct);
    Task MarkArchived(Guid eventId, CancellationToken ct);
}

public interface IAuditArchive
{
    Task Put(AuditEventRecord record, CancellationToken ct);
}