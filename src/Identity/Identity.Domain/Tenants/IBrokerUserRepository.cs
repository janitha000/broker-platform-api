namespace Identity.Domain.Tenants;

public interface IBrokerUserRepository
{
    Task<BrokerUser?> GetById(Guid id, CancellationToken cancellationToken = default);

    Task<BrokerUser?> GetByEmail(string email, CancellationToken cancellationToken = default);

    Task<BrokerUser> Add(BrokerUser brokerUser, CancellationToken cancellationToken = default);
}