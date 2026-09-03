namespace Identity.Application.Abstractions;

public enum Auth0ProvisionKind { Succeeded, Failed }

public sealed record Auth0ProvisionResult(Auth0ProvisionKind Kind, string? UserId);

public interface IAuth0UserDirectory
{
    Task<Auth0ProvisionResult> ProvisionUser(
        string email,
        string password,
        Guid tenantId,
        Guid brokerId,
        CancellationToken cancellationToken = default);
}