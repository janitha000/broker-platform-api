namespace Identity.Application.Tenants.RegisterTenant;

public sealed record RegisterCardDetails(
    string Number,
    int ExpMonth,
    int ExpYear,
    string Cvc);

public sealed record RegisterTenantCommand(
    string Name,
    string Email,
    string Password,
    RegisterCardDetails Card,
    string IdempotencyKey = "");