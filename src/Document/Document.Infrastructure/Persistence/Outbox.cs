using Document.Domain.Outbox;
using Document.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Origination.Infrastructure.Persistence;

public sealed class Outbox : IOutbox
{
    private readonly DocumentDbContext _context;

    public Outbox(DocumentDbContext context)
    {
        _context = context;
    }

    public Task<bool> Exists(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (_context.OutboxMessages.Local.Any(m => m.IdempotencyKey == idempotencyKey))
            return Task.FromResult(true);

        return _context.OutboxMessages.AnyAsync(m => m.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public void Add(OutboxMessage message) =>
        _context.OutboxMessages.Add(message);
}