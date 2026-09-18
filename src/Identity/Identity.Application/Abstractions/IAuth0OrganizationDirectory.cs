namespace Identity.Application.Abstractions;

public sealed record Auth0OrganizationResult(bool Succeeded, string? OrganizationId);

public interface IAuth0OrganizationDirectory
{
    Task<Auth0OrganizationResult> CreateOrganization(
        string displayName,
        CancellationToken cancellationToken = default);

    Task<bool> AddMemberWithPrincipalRole(
        string organizationId,
        string auth0UserId,
        CancellationToken cancellationToken = default);

    Task EnsureAuditReadPermission(CancellationToken cancellationToken = default);
}