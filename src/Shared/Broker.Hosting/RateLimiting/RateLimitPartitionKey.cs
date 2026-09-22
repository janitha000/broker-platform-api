using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Broker.Hosting.RateLimiting;

public static class RateLimitPartitionKey
{
    public const string TenantClaim = "tenant_id";
    public const string NamespacedTenantClaim = "https://api.broker-platform.com/tenant_id";

    public static string For(HttpContext http)
    {
        var user = http.User;
        var tenant = user.FindFirst(TenantClaim)?.Value
            ?? user.FindFirst(NamespacedTenantClaim)?.Value;
        if (!string.IsNullOrWhiteSpace(tenant))
            return $"t:{tenant}";

        var sub = user.FindFirst("sub")?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrWhiteSpace(sub) && user.Identity?.IsAuthenticated == true)
            return $"c:{sub}";

        var ip = http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"ip:{ip}";
    }

    public static bool IsHealth(HttpContext http)
    {
        var path = http.Request.Path.Value ?? "";
        return path.Equals("/health", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/health/", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsAuth(HttpContext http)
    {
        var path = http.Request.Path.Value ?? "";
        return path.Equals("/auth", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/auth/", StringComparison.OrdinalIgnoreCase);
    }
}
