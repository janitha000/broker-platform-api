using System.Security.Claims;
using Origination.Application.Auth;

namespace Origination.Api.Auth;

public static class OriginationAuth
{
    public const string ReadPolicy = "CasesRead";
    public const string CreatePolicy = "CasesCreate";
    public const string FactFindPolicy = "CasesFactFind";
    public const string LodgePolicy = "CasesLodge";
    public const string SettlePolicy = "CasesSettle";

    public static bool HasPermission(ClaimsPrincipal user, string permission)
    {
        if (user.FindAll(CasePermissions.ClaimType).Any(c => c.Value == permission))
            return true;

        var scope = user.FindFirst("scope")?.Value;
        if (string.IsNullOrEmpty(scope))
            return false;

        return scope.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Contains(permission);
    }
}
