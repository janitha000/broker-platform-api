using Microsoft.EntityFrameworkCore;
using Origination.Domain.Cases;

namespace Origination.Infrastructure.Persistence;

public sealed class CaseRepository : ICaseRepository
{
    private readonly OriginationDbContext _context;

    public CaseRepository(OriginationDbContext context)
    {
        _context = context;
    }

    public async Task<Case?> GetById(Guid caseId, Guid tenantId, CancellationToken cancellationToken = default)
    {
         return await _context.Cases
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == caseId && c.TenantId == tenantId, cancellationToken);
    }

    public async Task<IEnumerable<Case>> GetCasesByTenantId(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Cases
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task Add(Case @case, CancellationToken cancellationToken = default)
    {
        _context.Cases.Add(@case);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task Update(Case @case, CancellationToken cancellationToken = default)
    {
        _context.Cases.Update(@case);
        await _context.SaveChangesAsync(cancellationToken);
    }
}