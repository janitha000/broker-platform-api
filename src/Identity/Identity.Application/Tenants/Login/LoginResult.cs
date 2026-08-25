namespace Identity.Application.Tenants.Login;

public sealed record LoginResult(
    Guid TenantId,
    Guid BrokerId,
    string Email,
    string AccessToken);
