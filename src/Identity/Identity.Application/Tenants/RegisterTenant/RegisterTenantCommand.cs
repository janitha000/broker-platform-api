namespace Identity.Application.Tenants.RegisterTenant;

public sealed record RegisterTenantCommand(
    string Name,
    string Email,
    string Password) 