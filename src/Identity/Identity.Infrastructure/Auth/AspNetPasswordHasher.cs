using Identity.Application.Abstractions;
using Identity.Domain.Tenants;
using Microsoft.AspNetCore.Identity;

namespace Identity.Infrastructure.Auth;

public sealed class AspNetPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<BrokerUser> _hasher = new();

    public string Hash(string password) =>
        _hasher.HashPassword(new BrokerUser(), password);

    public bool Verify(string passwordHash, string password) =>
        _hasher.VerifyHashedPassword(new BrokerUser(), passwordHash, password)
            is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
}
