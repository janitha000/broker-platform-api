using System.Collections.Concurrent;
using Payment.Domain.Charges;

namespace Payment.Infrastructure.Persistence;

public sealed class InMemoryChargeRepository : IChargeRepository
{
    private readonly ConcurrentDictionary<string, Charge> _byKey = new(StringComparer.Ordinal);

    public Task<Charge?> GetByIdempotencyKey(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        _byKey.TryGetValue(idempotencyKey, out var charge);
        return Task.FromResult(charge);
    }

    public Task<Charge> Add(Charge charge, CancellationToken cancellationToken = default)
    {
        if (!_byKey.TryAdd(charge.IdempotencyKey, charge))
            throw new InvalidOperationException("Idempotency key already stored.");
        return Task.FromResult(charge);
    }
}
