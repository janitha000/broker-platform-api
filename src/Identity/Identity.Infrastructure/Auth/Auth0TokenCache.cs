namespace Identity.Infrastructure.Auth;

public class Auth0TokenCache
{
    public SemaphoreSlim Gate { get; } = new(1, 1);
    public string? AccessToken { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}

public sealed class Auth0ManagementTokenCache : Auth0TokenCache
{
}

public sealed class Auth0PaymentTokenCache : Auth0TokenCache
{
}
