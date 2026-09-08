using System.Security.Claims;

namespace Payment.Api.Auth;

public static class PaymentAuth
{
    public const string ChargePolicy = "PaymentCharge";
    public const string ChargePermission = "payments:charge";

    public const string RefundPolicy = "PaymentRefund";
    public const string RefundPermission = "payments:refund";

    public static bool HasChargePermission(ClaimsPrincipal user) =>
        HasPermission(user, ChargePermission);

    public static bool HasRefundPermission(ClaimsPrincipal user) =>
        HasPermission(user, RefundPermission);

    private static bool HasPermission(ClaimsPrincipal user, string permission)
    {
        if (user.FindAll("permissions").Any(c => c.Value == permission))
            return true;

        var scope = user.FindFirst("scope")?.Value;
        if (string.IsNullOrEmpty(scope))
            return false;

        return scope.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Contains(permission);
    }
}
