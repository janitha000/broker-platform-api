using Broker.Hosting.Audit;
using Identity.Domain.Outbox;
using Identity.Infrastructure.Persistence;

namespace Identity.Infrastructure.Messaging;

public sealed class OutboxAuditRecorder : IAuditRecorder
{
    private readonly IOutbox _outbox;
    private readonly IdentityDbContext _context;

    public OutboxAuditRecorder(IOutbox outbox, IdentityDbContext context)
    {
        _outbox = outbox;
        _context = context;
    }

    public void Record(AuditEvent auditEvent)
    {
        var eventId = auditEvent.EventId == Guid.Empty ? Guid.NewGuid() : auditEvent.EventId;
        auditEvent.EventId = eventId;
        if (auditEvent.OccurredAt == default)
            auditEvent.OccurredAt = DateTime.UtcNow;

        var key = $"audit:{eventId:D}";
        _outbox.Add(new OutboxMessage
        {
            Id = eventId,
            Type = AuditEventTypes.AuditEvent,
            Payload = AuditEventJson.Serialize(auditEvent),
            IdempotencyKey = key,
            OccurredAt = auditEvent.OccurredAt,
        });
    }

    public Task Flush(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
