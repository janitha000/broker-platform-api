using Identity.Application.Abstractions;
using Identity.Domain.Tenants;

namespace Identity.Application.Tenants.RegisterTenant;

public sealed class RegisterTenantHandler
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IBrokerUserRepository _brokerUserRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenIssuer _tokenIssuer;

    public RegisterTenantHandler(
        ITenantRepository tenantRepository,
        IBrokerUserRepository brokerUserRepository,
        IPasswordHasher passwordHasher,
        ITokenIssuer tokenIssuer)
    {
        _tenantRepository = tenantRepository;
        _brokerUserRepository = brokerUserRepository;
        _passwordHasher = passwordHasher;
        _tokenIssuer = tokenIssuer;
    }

    public async Task<RegisterTenantResult?> Handle(
        RegisterTenantCommand command,
        CancellationToken cancellationToken = default)
    {
        var email = command.Email.Trim();
        var existing = await _brokerUserRepository.GetByEmail(email, cancellationToken);
        if (existing is not null)
            return null;

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = command.Name.Trim(),
            CreatedAt = DateTime.UtcNow,
        };
        tenant = await _tenantRepository.Add(tenant, cancellationToken);

        var brokerUser = new BrokerUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Email = email,
            PasswordHash = _passwordHasher.Hash(command.Password),
            CreatedAt = DateTime.UtcNow,
        };
        brokerUser = await _brokerUserRepository.Add(brokerUser, cancellationToken);

        var accessToken = _tokenIssuer.Issue(brokerUser.Id, brokerUser.TenantId, brokerUser.Email);

        return new RegisterTenantResult(
            brokerUser.TenantId,
            brokerUser.Id,
            brokerUser.Email,
            accessToken);
    }
}
