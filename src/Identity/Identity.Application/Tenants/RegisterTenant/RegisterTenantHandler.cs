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

    public RegisterTenantHandler(
        ITenantRepository tenantRepository,
        IBrokerUserRepository brokerUserRepository,
        IPasswordHasher passwordHasher,
        ITokenIssuer tokenIssuer,
        IPaymentGateway paymentGateway)
    {
        _tenantRepository = tenantRepository;
        _brokerUserRepository = brokerUserRepository;
        _passwordHasher = passwordHasher;
        _tokenIssuer = tokenIssuer;
        _paymentGateway = paymentGateway;
    }

    public async Task<RegisterTenantOutcome> Handle(
        RegisterTenantCommand command,
        CancellationToken cancellationToken = default)
    {
        var email = command.Email.Trim().ToLowerInvariant();
        var existing = await _brokerUserRepository.GetByEmail(email, cancellationToken);
        if (existing is not null)
            return new RegisterTenantOutcome(RegisterTenantKind.DuplicateEmail, null);

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

        var accessToken = _tokenIssuer.Issue(brokerUser.Id, brokerUser.TenantId, brokerUser.Email);

        return new RegisterTenantOutcome(
            RegisterTenantKind.Succeeded,
            new RegisterTenantResult(
                brokerUser.TenantId,
                brokerUser.Id,
                brokerUser.Email,
                accessToken));
    }
}