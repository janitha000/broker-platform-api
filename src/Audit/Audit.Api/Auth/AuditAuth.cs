using System.Security.Claims;
using Audit.Application.Auth;

namespace Audit.Api.Auth;

public static class AuditAuth
{
    public const string ReadPolicy = "AuditRead";

    public static bool HasPermission(ClaimsPrincipal user, string permission)
    {
        if (user.FindAll(AuditPermissions.ClaimType).Any(c => c.Value == permission))
            return true;

        var scope = user.FindFirst("scope")?.Value;
        if (string.IsNullOrEmpty(scope))
            return false;

        return scope.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Contains(permission);
    }
}
