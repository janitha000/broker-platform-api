using Broker.Hosting.Audit;
using Identity.Application.Abstractions;
using Identity.Domain.Registration;
using Identity.Domain.Tenants;
using Microsoft.Extensions.Options;

namespace Identity.Application.Tenants.RegisterTenant;

public sealed class RegisterTenantHandler
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IBrokerUserRepository _brokerUserRepository;
    private readonly IRegistrationSagaRepository _sagas;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenIssuer _tokenIssuer;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IAuth0UserDirectory _auth0UserDirectory;
    private readonly IAuth0OrganizationDirectory _auth0Organizations;
    private readonly AuthOptions _auth;
    private readonly IAuditRecorder _audit;

    public RegisterTenantHandler(
        ITenantRepository tenantRepository,
        IBrokerUserRepository brokerUserRepository,
        IRegistrationSagaRepository sagas,
        IPasswordHasher passwordHasher,
        ITokenIssuer tokenIssuer,
        IPaymentGateway paymentGateway,
        IAuth0UserDirectory auth0UserDirectory,
        IAuth0OrganizationDirectory auth0Organizations,
        IOptions<AuthOptions> auth,
        IAuditRecorder audit)
    {
        _tenantRepository = tenantRepository;
        _brokerUserRepository = brokerUserRepository;
        _sagas = sagas;
        _passwordHasher = passwordHasher;
        _tokenIssuer = tokenIssuer;
        _paymentGateway = paymentGateway;
        _auth0UserDirectory = auth0UserDirectory;
        _auth0Organizations = auth0Organizations;
        _auth = auth.Value;
        _audit = audit;
    }

    public async Task<RegisterTenantOutcome> Handle(
        RegisterTenantCommand command,
        CancellationToken cancellationToken = default)
    {
        var email = command.Email.Trim().ToLowerInvariant();
        var key = command.IdempotencyKey.Trim();
        if (string.IsNullOrEmpty(key))
            key = Guid.NewGuid().ToString("N");

        var existing = await _brokerUserRepository.GetByEmail(email, cancellationToken);
        if (existing is not null)
        {
            if (!string.IsNullOrEmpty(existing.Auth0UserId))
                return new RegisterTenantOutcome(RegisterTenantKind.DuplicateEmail, null);

            var resume = await _sagas.GetByIdempotencyKey(key, cancellationToken);
            if (resume is not null && !string.Equals(resume.Email, email, StringComparison.Ordinal))
                resume = null;

            return await AttachAuth0(existing, command.Password, resume, cancellationToken);
        }

        var saga = await _sagas.GetByIdempotencyKey(key, cancellationToken);
        if (saga is null)
        {
            saga = await _sagas.Add(new RegistrationSaga
            {
                Id = Guid.NewGuid(),
                IdempotencyKey = key,
                Email = email,
                Status = RegistrationSagaStatus.Started,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }, cancellationToken);
        }

        if (saga.Status == RegistrationSagaStatus.Completed)
            return await IssueForSaga(saga, cancellationToken);

        if (saga.Status == RegistrationSagaStatus.Failed)
            return new RegisterTenantOutcome(ParseFailedKind(saga.LastError), null);

        if (saga.Status is RegistrationSagaStatus.Compensated
            or RegistrationSagaStatus.Compensating)
            return new RegisterTenantOutcome(RegisterTenantKind.IdentityProviderUnavailable, null);

        return await Advance(saga, command, cancellationToken);
    }

    private async Task<RegisterTenantOutcome> Advance(
        RegistrationSaga saga,
        RegisterTenantCommand command,
        CancellationToken cancellationToken)
    {
        if (saga.Status == RegistrationSagaStatus.Started)
        {
            var payment = await _paymentGateway.Charge(
                saga.Email,
                new PaymentCard(
                    command.Card.Number,
                    command.Card.ExpMonth,
                    command.Card.ExpYear,
                    command.Card.Cvc),
                saga.IdempotencyKey,
                cancellationToken);

            if (payment.Status == PaymentChargeStatus.Declined)
                return await Fail(saga, RegisterTenantKind.PaymentDeclined, cancellationToken);
            if (payment.Status == PaymentChargeStatus.Conflict)
                return await Fail(saga, RegisterTenantKind.PaymentConflict, cancellationToken);
            if (payment.Status != PaymentChargeStatus.Succeeded || payment.ChargeId is null)
                return new RegisterTenantOutcome(RegisterTenantKind.PaymentUnavailable, null);

            saga.ChargeId = payment.ChargeId;
            saga.Status = RegistrationSagaStatus.Charged;
            await _sagas.Update(saga, cancellationToken);
        }

        if (saga.Status == RegistrationSagaStatus.Charged)
        {
            if (saga.BrokerUserId is null)
            {
                var tenant = await _tenantRepository.Add(new Tenant
                {
                    Id = Guid.NewGuid(),
                    Name = command.Name.Trim(),
                    CreatedAt = DateTime.UtcNow,
                }, cancellationToken);

                var created = await _brokerUserRepository.Add(new BrokerUser
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    Email = saga.Email,
                    PasswordHash = _passwordHasher.Hash(command.Password),
                    CreatedAt = DateTime.UtcNow,
                    Role = BrokerRole.Principal,
                }, cancellationToken);

                saga.TenantId = tenant.Id;
                saga.BrokerUserId = created.Id;
            }

            saga.Status = RegistrationSagaStatus.TenantCreated;
            await _sagas.Update(saga, cancellationToken);
        }

        if (saga.Status == RegistrationSagaStatus.TenantCreated)
        {
            var user = await _brokerUserRepository.GetById(saga.BrokerUserId!.Value, cancellationToken);
            return await AttachAuth0(user!, command.Password, saga, cancellationToken);
        }

        return new RegisterTenantOutcome(RegisterTenantKind.IdentityProviderUnavailable, null);
    }

    private async Task<RegisterTenantOutcome> AttachAuth0(
        BrokerUser user,
        string password,
        RegistrationSaga? saga,
        CancellationToken cancellationToken)
    {
        var provision = await _auth0UserDirectory.ProvisionUser(
            user.Email,
            password,
            user.TenantId,
            user.Id,
            cancellationToken);

        if (provision.Kind != Auth0ProvisionKind.Succeeded || provision.UserId is null)
        {
            if (saga?.ChargeId is Guid chargeId)
                await Compensate(saga, chargeId, cancellationToken);

            return new RegisterTenantOutcome(RegisterTenantKind.IdentityProviderUnavailable, null);
        }

        if (_auth.UseAuth0Organizations)
        {
            var attached = await AttachOrganization(user, provision.UserId, saga, cancellationToken);
            if (!attached)
            {
                if (saga?.ChargeId is Guid chargeId)
                    await Compensate(saga, chargeId, cancellationToken);

                return new RegisterTenantOutcome(RegisterTenantKind.IdentityProviderUnavailable, null);
            }
        }

        user.Auth0UserId = provision.UserId;
        await _brokerUserRepository.Update(user, cancellationToken);

        var justCompleted = false;
        if (saga is not null)
        {
            saga.Auth0UserId = provision.UserId;
            justCompleted = saga.Status != RegistrationSagaStatus.Completed;
            saga.Status = RegistrationSagaStatus.Completed;
            saga.CompletedAt = DateTime.UtcNow;
            await _sagas.Update(saga, cancellationToken);
        }

        if (justCompleted)
        {
            _audit.Record(new AuditEvent
            {
                EventId = Guid.NewGuid(),
                OccurredAt = DateTime.UtcNow,
                TenantId = user.TenantId,
                Action = AuditActions.IdentityTenantRegistered,
                Outcome = AuditOutcomes.Allow,
                Actor = new AuditActor
                {
                    Type = AuditActorTypes.User,
                    BrokerId = user.Id,
                },
                Resource = new AuditResource
                {
                    Type = AuditResourceTypes.Tenant,
                    Id = user.TenantId.ToString("D"),
                },
            });
            await _audit.Flush(cancellationToken);
        }

        var accessToken = _tokenIssuer.Issue(user.Id, user.TenantId, user.Email, user.Role);
        return new RegisterTenantOutcome(
            RegisterTenantKind.Succeeded,
            new RegisterTenantResult(user.TenantId, user.Id, user.Email, accessToken, user.Role));
    }

    private async Task<bool> AttachOrganization(
        BrokerUser user,
        string auth0UserId,
        RegistrationSaga? saga,
        CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetById(user.TenantId, cancellationToken);
        if (tenant is null)
            return false;

        var orgId = saga?.Auth0OrganizationId ?? tenant.Auth0OrganizationId;
        if (string.IsNullOrWhiteSpace(orgId))
        {
            var created = await _auth0Organizations.CreateOrganization(tenant.Name, cancellationToken);
            if (!created.Succeeded || string.IsNullOrWhiteSpace(created.OrganizationId))
                return false;

            orgId = created.OrganizationId;
            tenant.Auth0OrganizationId = orgId;
            await _tenantRepository.Update(tenant, cancellationToken);
            if (saga is not null)
            {
                saga.Auth0OrganizationId = orgId;
                await _sagas.Update(saga, cancellationToken);
            }
        }

        return await _auth0Organizations.AddMemberWithPrincipalRole(orgId, auth0UserId, cancellationToken);
    }

    private async Task Compensate(
        RegistrationSaga saga,
        Guid chargeId,
        CancellationToken cancellationToken)
    {
        saga.Status = RegistrationSagaStatus.Compensating;
        saga.LastError = "Auth0 unavailable";
        await _sagas.Update(saga, cancellationToken);

        var refund = await _paymentGateway.Refund(chargeId, saga.Id.ToString(), cancellationToken);
        if (refund is PaymentRefundStatus.Succeeded or PaymentRefundStatus.NotRefundable)
        {
            saga.Status = RegistrationSagaStatus.Compensated;
            saga.CompletedAt = DateTime.UtcNow;
            await _sagas.Update(saga, cancellationToken);
        }
        // Unavailable: leave Compensating. Next register with this email still hits AttachAuth0.
    }

    private async Task<RegisterTenantOutcome> Fail(
        RegistrationSaga saga,
        RegisterTenantKind kind,
        CancellationToken cancellationToken)
    {
        saga.Status = RegistrationSagaStatus.Failed;
        saga.LastError = kind.ToString();
        saga.CompletedAt = DateTime.UtcNow;
        await _sagas.Update(saga, cancellationToken);
        return new RegisterTenantOutcome(kind, null);
    }

    private static RegisterTenantKind ParseFailedKind(string? lastError)
    {
        if (Enum.TryParse<RegisterTenantKind>(lastError, out var kind)
            && kind is RegisterTenantKind.PaymentDeclined or RegisterTenantKind.PaymentConflict)
            return kind;

        return RegisterTenantKind.PaymentUnavailable;
    }

    private async Task<RegisterTenantOutcome> IssueForSaga(
        RegistrationSaga saga,
        CancellationToken cancellationToken)
    {
        var user = await _brokerUserRepository.GetById(saga.BrokerUserId!.Value, cancellationToken);
        var token = _tokenIssuer.Issue(user!.Id, user.TenantId, user.Email, user.Role);
        return new RegisterTenantOutcome(
            RegisterTenantKind.Succeeded,
            new RegisterTenantResult(user.TenantId, user.Id, user.Email, token, user.Role));
    }
}