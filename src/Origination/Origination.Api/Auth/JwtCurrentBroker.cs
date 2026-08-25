using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Origination.Application.Abstractions;

namespace Origination.Api.Auth;

public sealed class JwtCurrentBroker : ICurrentBroker
{
    public const string TenantIdClaimType = "tenant_id";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public JwtCurrentBroker(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid BrokerId => ParseRequired(ClaimTypes.NameIdentifier, JwtRegisteredClaimNames.Sub);

    public Guid TenantId => ParseRequired(TenantIdClaimType);

    private Guid ParseRequired(params string[] claimTypes)
    {
        var user = _httpContextAccessor.HttpContext?.User
            ?? throw new InvalidOperationException("No HTTP context.");

        foreach (var type in claimTypes)
        {
            var value = user.FindFirst(type)?.Value;
            if (Guid.TryParse(value, out var id))
                return id;
        }

        throw new InvalidOperationException($"Missing broker claim ({string.Join(", ", claimTypes)}).");
    }
}
