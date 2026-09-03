using Identity.Application.Abstractions;
using Identity.Domain.Tenants;

namespace Identity.Application.Tenants.RegisterTenant;

public sealed class RegisterTenantHandler
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IBrokerUserRepository _brokerUserRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenIssuer _tokenIssuer;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IAuth0UserDirectory _auth0UserDirectory;

    public RegisterTenantHandler(
        ITenantRepository tenantRepository,
        IBrokerUserRepository brokerUserRepository,
        IPasswordHasher passwordHasher,
        ITokenIssuer tokenIssuer,
        IPaymentGateway paymentGateway,
        IAuth0UserDirectory auth0UserDirectory)
    {
        _tenantRepository = tenantRepository;
        _brokerUserRepository = brokerUserRepository;
        _passwordHasher = passwordHasher;
        _tokenIssuer = tokenIssuer;
        _paymentGateway = paymentGateway;
        _auth0UserDirectory = auth0UserDirectory;
    }

    public async Task<RegisterTenantOutcome> Handle(
        RegisterTenantCommand command,
        CancellationToken cancellationToken = default)
    {
        var email = command.Email.Trim().ToLowerInvariant();
        var existing = await _brokerUserRepository.GetByEmail(email, cancellationToken);
        if (existing is not null)
        {
            if (!string.IsNullOrEmpty(existing.Auth0UserId))
                return new RegisterTenantOutcome(RegisterTenantKind.DuplicateEmail, null);

            return await AttachAuth0(existing, command.Password, cancellationToken);
        }

        var payment = await _paymentGateway.Charge(
            email,
            new PaymentCard(
                command.Card.Number,
                command.Card.ExpMonth,
                command.Card.ExpYear,
                command.Card.Cvc),
            command.IdempotencyKey.Trim(),
            cancellationToken);

        if (payment == PaymentChargeStatus.Declined)
            return new RegisterTenantOutcome(RegisterTenantKind.PaymentDeclined, null);
        if (payment == PaymentChargeStatus.Conflict)
            return new RegisterTenantOutcome(RegisterTenantKind.PaymentConflict, null);
        if (payment != PaymentChargeStatus.Succeeded)
            return new RegisterTenantOutcome(RegisterTenantKind.PaymentUnavailable, null);

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

        return await AttachAuth0(brokerUser, command.Password, cancellationToken);
    }

    private async Task<RegisterTenantOutcome> AttachAuth0(
        BrokerUser user,
        string password,
        CancellationToken cancellationToken)
    {
        var provision = await _auth0UserDirectory.ProvisionUser(
            user.Email,
            password,
            user.TenantId,
            user.Id,
            cancellationToken);
        if (provision.Kind != Auth0ProvisionKind.Succeeded || provision.UserId is null)
            return new RegisterTenantOutcome(RegisterTenantKind.IdentityProviderUnavailable, null);

        user.Auth0UserId = provision.UserId;
        await _brokerUserRepository.Update(user, cancellationToken);

        var accessToken = _tokenIssuer.Issue(user.Id, user.TenantId, user.Email);
        return new RegisterTenantOutcome(
            RegisterTenantKind.Succeeded,
            new RegisterTenantResult(user.TenantId, user.Id, user.Email, accessToken));
    }
}
