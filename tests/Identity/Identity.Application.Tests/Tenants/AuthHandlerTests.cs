using Identity.Application.Abstractions;
using Identity.Application.Tenants.Login;
using Identity.Application.Tenants.RegisterTenant;
using Identity.Domain.Tenants;

namespace Identity.Application.Tests.Tenants;

public sealed class AuthHandlerTests
{
    [Fact]
    public async Task Register_CreatesTenantAndUser_AndReturnsToken()
    {
        var tenants = new InMemoryTenantRepository();
        var users = new InMemoryBrokerUserRepository();
        var hasher = new FakePasswordHasher();
        var tokens = new FakeTokenIssuer();
        var handler = new RegisterTenantHandler(tenants, users, hasher, tokens);

        var result = await handler.Handle(new RegisterTenantCommand(
            "  Example Brokers  ",
            "  Broker@Example.COM  ",
            "secret"));

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result!.TenantId);
        Assert.NotEqual(Guid.Empty, result.BrokerId);
        Assert.Equal("broker@example.com", result.Email);
        Assert.Equal($"{result.BrokerId}|{result.TenantId}|broker@example.com", result.AccessToken);

        var stored = await users.GetByEmail("broker@example.com");
        Assert.NotNull(stored);
        Assert.Equal(result.TenantId, stored!.TenantId);
        Assert.Equal("hash:secret", stored.PasswordHash);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsNull()
    {
        var tenants = new InMemoryTenantRepository();
        var users = new InMemoryBrokerUserRepository();
        var hasher = new FakePasswordHasher();
        var tokens = new FakeTokenIssuer();
        var handler = new RegisterTenantHandler(tenants, users, hasher, tokens);
        var command = new RegisterTenantCommand("Firm", "a@b.com", "pw");

        await handler.Handle(command);
        var second = await handler.Handle(command);

        Assert.Null(second);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsSameBrokerAndTenant()
    {
        var tenants = new InMemoryTenantRepository();
        var users = new InMemoryBrokerUserRepository();
        var hasher = new FakePasswordHasher();
        var tokens = new FakeTokenIssuer();
        var registered = await new RegisterTenantHandler(tenants, users, hasher, tokens)
            .Handle(new RegisterTenantCommand("Firm", "a@b.com", "pw"));

        var login = await new LoginHandler(users, hasher, tokens)
            .Handle(new LoginCommand("  A@B.COM  ", "pw"));

        Assert.NotNull(login);
        Assert.Equal(registered!.TenantId, login!.TenantId);
        Assert.Equal(registered.BrokerId, login.BrokerId);
        Assert.Equal("a@b.com", login.Email);
        Assert.Equal(registered.AccessToken, login.AccessToken);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsNull()
    {
        var tenants = new InMemoryTenantRepository();
        var users = new InMemoryBrokerUserRepository();
        var hasher = new FakePasswordHasher();
        var tokens = new FakeTokenIssuer();
        await new RegisterTenantHandler(tenants, users, hasher, tokens)
            .Handle(new RegisterTenantCommand("Firm", "a@b.com", "pw"));

        var login = await new LoginHandler(users, hasher, tokens)
            .Handle(new LoginCommand("a@b.com", "other"));

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
