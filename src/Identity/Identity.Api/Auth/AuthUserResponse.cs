namespace Identity.Api.Auth;

public sealed record AuthUserResponse(
    Guid TenantId,
    Guid BrokerId,
    string Email,
    string Role,
    IReadOnlyList<string> Permissions);