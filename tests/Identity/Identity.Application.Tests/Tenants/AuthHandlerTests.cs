using Identity.Application.Abstractions;
using Identity.Application.Tenants.CompleteAuth0Login;
using Identity.Application.Tenants.Login;
using Identity.Application.Tenants.RegisterTenant;
using Identity.Domain.Tenants;

namespace Identity.Application.Tests.Tenants;

public sealed class AuthHandlerTests
{
    private static RegisterCardDetails ValidCard(string number = "4242424242424242") =>
        new(number, 12, 2030, "123");

    private static RegisterTenantCommand RegisterCommand(
        string email = "a@b.com",
        string cardNumber = "4242424242424242",
        string key = "key-1") =>
        new("Firm", email, "pw", ValidCard(cardNumber), key);

    private static RegisterTenantHandler RegisterHandler(
        IBrokerUserRepository? users = null,
        IPaymentGateway? payment = null,
        ITenantRepository? tenants = null) =>
        new(
            tenants ?? new InMemoryTenantRepository(),
            users ?? new InMemoryBrokerUserRepository(),
            new FakePasswordHasher(),
            new FakeTokenIssuer(),
            payment ?? new StubPaymentGateway(PaymentChargeStatus.Succeeded));

    [Fact]
    public async Task Register_CreatesTenantAndUser_AndReturnsToken()
    {
        var users = new InMemoryBrokerUserRepository();
        var outcome = await RegisterHandler(users).Handle(
            new RegisterTenantCommand(
                "  Example Brokers  ",
                "  Broker@Example.COM  ",
                "secret",
                ValidCard(),
                "key-1"));

        Assert.Equal(RegisterTenantKind.Succeeded, outcome.Kind);
        var result = outcome.Result!;
        Assert.Equal("broker@example.com", result.Email);
        Assert.Equal($"{result.BrokerId}|{result.TenantId}|broker@example.com", result.AccessToken);

        var stored = await users.GetByEmail("broker@example.com");
        Assert.NotNull(stored);
        Assert.Equal(result.TenantId, stored!.TenantId);
        Assert.Equal("hash:secret", stored.PasswordHash);
    }

    [Fact]
    public async Task Register_DuplicateEmail_DoesNotCharge()
    {
        var users = new InMemoryBrokerUserRepository();
        var payment = new CountingPaymentGateway(PaymentChargeStatus.Succeeded);
        var handler = RegisterHandler(users, payment);
        var command = RegisterCommand();

        await handler.Handle(command);
        var second = await handler.Handle(command);

        Assert.Equal(RegisterTenantKind.DuplicateEmail, second.Kind);
        Assert.Equal(1, payment.Calls);
    }

    [Fact]
    public async Task Register_DeclinedCard_DoesNotCreateUser()
    {
        var users = new InMemoryBrokerUserRepository();
        var outcome = await RegisterHandler(
                users,
                new StubPaymentGateway(PaymentChargeStatus.Declined))
            .Handle(RegisterCommand());

        Assert.Equal(RegisterTenantKind.PaymentDeclined, outcome.Kind);
        Assert.Null(await users.GetByEmail("a@b.com"));
    }

    [Fact]
    public async Task Register_PaymentConflict_DoesNotCreateUser()
    {
        var outcome = await RegisterHandler(
                payment: new StubPaymentGateway(PaymentChargeStatus.Conflict))
            .Handle(RegisterCommand());

        Assert.Equal(RegisterTenantKind.PaymentConflict, outcome.Kind);
        Assert.Null(outcome.Result);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsSameBrokerAndTenant()
    {
        var users = new InMemoryBrokerUserRepository();
        var registered = await RegisterHandler(users).Handle(RegisterCommand());

        var login = await new LoginHandler(users, new FakePasswordHasher(), new FakeTokenIssuer())
            .Handle(new LoginCommand("  A@B.COM  ", "pw"));

        Assert.NotNull(login);
        Assert.Equal(registered.Result!.TenantId, login!.TenantId);
        Assert.Equal(registered.Result.BrokerId, login.BrokerId);
        Assert.Equal("a@b.com", login.Email);
        Assert.Equal(registered.Result.AccessToken, login.AccessToken);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsNull()
    {
        var users = new InMemoryBrokerUserRepository();
        await RegisterHandler(users).Handle(RegisterCommand());

        var login = await new LoginHandler(users, new FakePasswordHasher(), new FakeTokenIssuer())
            .Handle(new LoginCommand("a@b.com", "other"));

        Assert.Null(login);
    }

    [Fact]
    public async Task CompleteAuth0Login_KnownEmail_ReturnsSameBrokerAndTenant()
    {
        var users = new InMemoryBrokerUserRepository();
        var registered = await RegisterHandler(users).Handle(RegisterCommand());

        var login = await new CompleteAuth0LoginHandler(users, new FakeTokenIssuer())
            .Handle(new CompleteAuth0LoginCommand("  A@B.COM  ", "auth0|1"));

        Assert.NotNull(login);
        Assert.Equal(registered.Result!.TenantId, login!.TenantId);
        Assert.Equal(registered.Result.BrokerId, login.BrokerId);
        Assert.Equal("a@b.com", login.Email);
        Assert.Equal(registered.Result.AccessToken, login.AccessToken);
    }

    [Fact]
    public async Task CompleteAuth0Login_UnknownEmail_ReturnsNull()
    {
        var login = await new CompleteAuth0LoginHandler(
                new InMemoryBrokerUserRepository(),
                new FakeTokenIssuer())
            .Handle(new CompleteAuth0LoginCommand("missing@x.com", "auth0|1"));

        Assert.Null(login);
    }

    [Fact]
    public async Task Login_UnknownEmail_ReturnsNull()
    {
        var login = await new LoginHandler(
                new InMemoryBrokerUserRepository(),
                new FakePasswordHasher(),
                new FakeTokenIssuer())
            .Handle(new LoginCommand("missing@x.com", "pw"));

        Assert.Null(login);
    }
}

file sealed class StubPaymentGateway(PaymentChargeStatus status) : IPaymentGateway
{
    public Task<PaymentChargeStatus> Charge(
        string email,
        PaymentCard card,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(status);
}

file sealed class CountingPaymentGateway(PaymentChargeStatus status) : IPaymentGateway
{
    public int Calls { get; private set; }

    public Task<PaymentChargeStatus> Charge(
        string email,
        PaymentCard card,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        Calls++;
        return Task.FromResult(status);
    }
}

file sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"hash:{password}";

    public bool Verify(string passwordHash, string password) =>
        passwordHash == $"hash:{password}";
}

file sealed class FakeTokenIssuer : ITokenIssuer
{
    public string Issue(Guid brokerId, Guid tenantId, string email) =>
        $"{brokerId}|{tenantId}|{email}";
}

file sealed class InMemoryTenantRepository : ITenantRepository
{
    private readonly Dictionary<Guid, Tenant> _tenants = new();

    public Task<Tenant> Add(Tenant tenant, CancellationToken cancellationToken = default)
    {
        _tenants[tenant.Id] = tenant;
        return Task.FromResult(tenant);
    }

    public Task<Tenant?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        _tenants.TryGetValue(id, out var tenant);
        return Task.FromResult(tenant);
    }
}

file sealed class InMemoryBrokerUserRepository : IBrokerUserRepository
{
    private readonly Dictionary<Guid, BrokerUser> _users = new();

    public Task<BrokerUser> Add(BrokerUser brokerUser, CancellationToken cancellationToken = default)
    {
        _users[brokerUser.Id] = brokerUser;
        return Task.FromResult(brokerUser);
    }

    public Task<BrokerUser?> GetByEmail(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var user = _users.Values.FirstOrDefault(u => u.Email == normalized);
        return Task.FromResult(user);
    }

    public Task<BrokerUser?> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        _users.TryGetValue(id, out var user);
        return Task.FromResult(user);
    }
}
