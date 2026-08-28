namespace Origination.Domain.Cases;

public interface ICaseRepository
{
    Task<Case?> GetById(Guid caseId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Case>> GetCasesByTenantId(Guid tenantId, CancellationToken cancellationToken = default);
    Task Add(Case @case, CancellationToken cancellationToken = default);
    Task Update(Case @case, CancellationToken cancellationToken = default);

}