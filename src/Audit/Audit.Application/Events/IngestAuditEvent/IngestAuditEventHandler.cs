using System.Security.Cryptography;
using System.Text;
using Audit.Application.Abstractions;
using Audit.Domain.Events;
using Broker.Hosting.Audit;

namespace Audit.Application.Events.IngestAuditEvent;

public sealed class IngestAuditEventHandler
{
    private readonly IAuditStore _store;
    private readonly IAuditArchive _archive;

    public IngestAuditEventHandler(IAuditStore store, IAuditArchive archive)
    {
        _store = store;
        _archive = archive;
    }

    public async Task Handle(AuditEvent envelope, CancellationToken ct)
    {
        if (envelope.EventId == Guid.Empty)
            throw new InvalidOperationException("AuditEvent.EventId is required.");

        envelope.Actor ??= new AuditActor();
        envelope.Resource ??= new AuditResource();

        var existing = await _store.GetByEventId(envelope.EventId, ct);
        if (existing is not null)
        {
            if (existing.ArchivedAt is null)
            {
                await _archive.Put(existing, ct);
                await _store.MarkArchived(existing.EventId, ct);
            }

            return;
        }

        var envelopeJson = AuditEventJson.Serialize(envelope);
        var record = new AuditEventRecord
        {
            EventId = envelope.EventId,
            OccurredAt = envelope.OccurredAt == default ? DateTime.UtcNow : envelope.OccurredAt,
            IngestedAt = DateTime.UtcNow,
            TenantId = envelope.TenantId,
            Action = envelope.Action,
            Outcome = envelope.Outcome,
            ActorType = envelope.Actor.Type,
            BrokerId = envelope.Actor.BrokerId,
            Subject = envelope.Actor.Subject,
            IpAddress = envelope.Actor.IpAddress,
            UserAgent = envelope.Actor.UserAgent,
            ResourceType = envelope.Resource.Type,
            ResourceId = envelope.Resource.Id,
            CaseId = envelope.Resource.CaseId,
            Sensitivity = envelope.Resource.Sensitivity,
            CorrelationId = envelope.CorrelationId,
            RequestId = envelope.RequestId,
            TraceId = envelope.TraceId,
            DataJson = envelope.DataJson,
            EnvelopeJson = envelopeJson,
        };

        var inserted = await _store.InsertChained(record, ct);
        await _archive.Put(inserted, ct);
        await _store.MarkArchived(inserted.EventId, ct);
    }
}

public static class AuditRecordHash
{
    public static string Compute(string? prevHash, string envelopeJson)
    {
        var bytes = Encoding.UTF8.GetBytes($"{prevHash}|{envelopeJson}");
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}