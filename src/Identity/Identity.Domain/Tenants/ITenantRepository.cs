namespace Identity.Domain.Tenants;

public interface ITenantRepository
{
    Task<Tenant?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<Tenant> Add(Tenant tenant, CancellationToken cancellationToken = default);
}