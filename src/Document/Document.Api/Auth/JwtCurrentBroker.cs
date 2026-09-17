using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Document.Application.Abstractions;

namespace Document.Api.Auth;

public sealed class JwtCurrentBroker : ICurrentBroker, ITenantContext
{
    public const string TenantIdClaimType = "tenant_id";
    public const string BrokerIdClaimType = "broker_id";
    public const string NamespacedTenantIdClaimType = "https://api.broker-platform.com/tenant_id";
    public const string NamespacedBrokerIdClaimType = "https://api.broker-platform.com/broker_id";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public JwtCurrentBroker(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid BrokerId => ParseRequired(
        BrokerIdClaimType,
        NamespacedBrokerIdClaimType,
        ClaimTypes.NameIdentifier,
        JwtRegisteredClaimNames.Sub);

    public Guid TenantId => ParseRequired(TenantIdClaimType, NamespacedTenantIdClaimType);

    Guid? ITenantContext.TenantId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            return TryParseGuid(user, TenantIdClaimType, NamespacedTenantIdClaimType)
                ?? Guid.Empty;
        }
    }

    public bool HasPermission(string permission)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user is null || string.IsNullOrEmpty(permission))
            return false;

        return DocumentAuth.HasPermission(user, permission);
    }

    private Guid ParseRequired(params string[] claimTypes)
    {
        var user = _httpContextAccessor.HttpContext?.User
            ?? throw new InvalidOperationException("No HTTP context.");

        var id = TryParseGuid(user, claimTypes);
        if (id is Guid parsed)
            return parsed;

        throw new InvalidOperationException($"Missing broker claim ({string.Join(", ", claimTypes)}).");
    }

    private static Guid? TryParseGuid(ClaimsPrincipal user, params string[] claimTypes)
    {
        foreach (var type in claimTypes)
        {
            var value = user.FindFirst(type)?.Value;
            if (Guid.TryParse(value, out var id))
                return id;
        }

        return null;
    }
}
