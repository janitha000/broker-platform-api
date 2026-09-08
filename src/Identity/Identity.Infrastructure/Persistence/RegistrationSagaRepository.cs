using Identity.Domain.Registration;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence;

public sealed class RegistrationSagaRepository : IRegistrationSagaRepository
{
    private readonly IdentityDbContext _context;

    public RegistrationSagaRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<RegistrationSaga?> GetByIdempotencyKey(
        string key,
        CancellationToken ct = default)
    {
        return await _context.RegistrationSagas
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.IdempotencyKey == key, ct);
    }

    public async Task<RegistrationSaga> Add(RegistrationSaga saga, CancellationToken ct = default)
    {
        _context.RegistrationSagas.Add(saga);
        await _context.SaveChangesAsync(ct);
        return saga;
    }

    public async Task Update(RegistrationSaga saga, CancellationToken ct = default)
    {
        saga.UpdatedAt = DateTime.UtcNow;
        _context.RegistrationSagas.Update(saga);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<RegistrationSaga>> GetIncomplete(
        DateTime olderThan,
        int take,
        CancellationToken ct = default)
    {
        var open = new[]
        {
            RegistrationSagaStatus.Started,
            RegistrationSagaStatus.Charged,
            RegistrationSagaStatus.TenantCreated,
            RegistrationSagaStatus.Compensating,
        };

        return await _context.RegistrationSagas
            .AsNoTracking()
            .Where(s => open.Contains(s.Status) && s.UpdatedAt < olderThan)
            .OrderBy(s => s.UpdatedAt)
            .Take(take)
            .ToListAsync(ct);
    }
}
