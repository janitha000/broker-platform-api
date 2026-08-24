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

    public async Task<Case?> GetById(Guid caseId, CancellationToken cancellationToken = default)
    {
         return await _context.Cases
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken);
    }

    public async Task Add(Case @case, CancellationToken cancellationToken = default)
    {
        _context.Cases.Add(@case);
        await _context.SaveChangesAsync(cancellationToken);
    }
}