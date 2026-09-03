using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Payment.Api.Auth;

public static class PaymentAuth
{
    public const string ChargePolicy = "PaymentCharge";
    public const string ChargePermission = "payments:charge";

    public static bool HasChargePermission(ClaimsPrincipal user)
    {
        if (user.FindAll("permissions").Any(c => c.Value == ChargePermission))
            return true;

        var scope = user.FindFirst("scope")?.Value;
        if (string.IsNullOrEmpty(scope))
            return false;

        return scope.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Contains(ChargePermission);
    }
}
