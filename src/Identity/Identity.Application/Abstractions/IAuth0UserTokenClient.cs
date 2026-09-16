namespace Identity.Application.Abstractions;

public sealed record Auth0RefreshedTokens(
    string AccessToken,
    string? RefreshToken,
    int ExpiresInSeconds);

public interface IAuth0UserTokenClient
{
    Task<Auth0RefreshedTokens?> Refresh(
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task Revoke(
        string refreshToken,
        CancellationToken cancellationToken = default);
}