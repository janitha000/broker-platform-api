using Broker.Hosting.Telemetry;
using Microsoft.EntityFrameworkCore;
using Origination.Domain.Outbox;

namespace Origination.Infrastructure.Persistence;

public sealed class Outbox : IOutbox
{
    private readonly OriginationDbContext _context;

    public Outbox(OriginationDbContext context)
    {
        _context = context;
    }

    public Task<bool> Exists(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (_context.OutboxMessages.Local.Any(m => m.IdempotencyKey == idempotencyKey))
            return Task.FromResult(true);

        return _context.OutboxMessages.AnyAsync(m => m.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public void Add(OutboxMessage message)
    {
        if (string.IsNullOrEmpty(message.TraceParent))
        {
            TraceContext.Capture(out var traceParent, out var traceState);
            message.TraceParent = traceParent;
            message.TraceState = traceState;
        }

        _context.OutboxMessages.Add(message);
    }
}