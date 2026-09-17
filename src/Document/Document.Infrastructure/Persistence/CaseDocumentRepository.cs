using Document.Domain.Documents;
using Microsoft.EntityFrameworkCore;

namespace Document.Infrastructure.Persistence;

public sealed class CaseDocumentRepository : ICaseDocumentRepository
{
    private readonly DocumentDbContext _context;

    public CaseDocumentRepository(DocumentDbContext context)
    {
        _context = context;
    }

    public Task<CaseDocument?> GetById(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        _context.CaseDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

    public Task<CaseDocument?> GetByIdIgnoringTenant(
        Guid id,
        CancellationToken cancellationToken = default) =>
        _context.CaseDocuments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<CaseDocument?> GetByIdempotencyKey(
        Guid tenantId,
        string key,
        CancellationToken cancellationToken = default) =>
        _context.CaseDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId && x.IdempotencyKey == key,
                cancellationToken);

    public async Task<IReadOnlyList<CaseDocument>> ListByCase(
        Guid tenantId,
        Guid caseId,
        CancellationToken cancellationToken = default) =>
        await _context.CaseDocuments
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.CaseId == caseId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CaseDocument>> ListPendingScan(
        int take,
        CancellationToken cancellationToken = default) =>
        await _context.CaseDocuments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.Status == DocumentStatus.PendingScan)
            .OrderBy(x => x.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task Add(CaseDocument document, CancellationToken cancellationToken = default)
    {
        _context.CaseDocuments.Add(document);
        return Task.CompletedTask;
    }

    public Task Update(CaseDocument document, CancellationToken cancellationToken = default)
    {
        _context.CaseDocuments.Update(document);
        return Task.CompletedTask;
    }
}
