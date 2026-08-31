namespace Identity.Application.Tenants.RegisterTenant;

public enum RegisterTenantKind
{
    Succeeded,
    DuplicateEmail,
    PaymentDeclined,
    PaymentConflict,
    PaymentUnavailable,
}

public sealed record RegisterTenantOutcome(
    RegisterTenantKind Kind,
    RegisterTenantResult? Result);