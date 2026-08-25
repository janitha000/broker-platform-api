namespace Identity.Application.Tenants.RegisterTenant;

public sealed record RegisterTenantResult(
    Guid TenantId,
    Guid BrokerId,
    string Email,
    string AccessToken);
