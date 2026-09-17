using Audit.Application.Abstractions;
using Audit.Application.Events.IngestAuditEvent;
using Audit.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace Audit.Infrastructure.Persistence;

public sealed class AuditStore : IAuditStore
{
    private readonly AuditDbContext _db;

    public AuditStore(AuditDbContext db)
    {
        _db = db;
    }

    public Task<AuditEventRecord?> GetByEventId(Guid eventId, CancellationToken ct) =>
        _db.AuditEvents.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId, ct);

    public async Task<AuditEventRecord> InsertChained(AuditEventRecord record, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var head = await _db.TenantAuditHeads
            .FirstOrDefaultAsync(h => h.TenantId == record.TenantId, ct);

        record.PrevHash = head?.LastHash;
        record.RecordHash = AuditRecordHash.Compute(record.PrevHash, record.EnvelopeJson);
        _db.AuditEvents.Add(record);

        if (head is null)
        {
            _db.TenantAuditHeads.Add(new TenantAuditHead
            {
                TenantId = record.TenantId,
                LastEventId = record.EventId,
                LastHash = record.RecordHash,
            });
        }
        else
        {
            head.LastEventId = record.EventId;
            head.LastHash = record.RecordHash;
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return record;
    }

    public async Task MarkArchived(Guid eventId, CancellationToken ct)
    {
        var row = await _db.AuditEvents.FirstAsync(e => e.EventId == eventId, ct);
        if (row.ArchivedAt is not null)
            return;

        row.ArchivedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}