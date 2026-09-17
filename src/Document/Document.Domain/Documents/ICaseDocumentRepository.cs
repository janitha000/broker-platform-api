namespace Document.Domain.Documents;

public interface ICaseDocumentRepository
{
    Task<CaseDocument?> GetById(Guid id, Guid tenantId, CancellationToken cancellationToken = default);
    Task<CaseDocument?> GetByIdIgnoringTenant(Guid id, CancellationToken cancellationToken = default);
    Task<CaseDocument?> GetByIdempotencyKey(Guid tenantId, string key, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CaseDocument>> ListByCase(Guid tenantId, Guid caseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CaseDocument>> ListPendingScan(int take, CancellationToken cancellationToken = default);
    Task Add(CaseDocument document, CancellationToken cancellationToken = default);
    Task Update(CaseDocument document, CancellationToken cancellationToken = default);
}
