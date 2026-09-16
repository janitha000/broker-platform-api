using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Broker.Hosting.Auth;

public static class AuthCookie
{
    public const string Name = "broker.access";
    public const string RefreshName = "broker.refresh";

    /// <summary>HMAC session cookie in IdentityJwt mode.</summary>
    public static readonly TimeSpan AccessLifetime = TimeSpan.FromHours(8);
    /// <summary>Auth0 access tokens are typically ~1 hour.</summary>
    public static readonly TimeSpan Auth0AccessLifetime = TimeSpan.FromHours(1);
    /// <summary>Refresh cookie idle window; must stay within Auth0 refresh absolute/idle settings.</summary>
    public static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(7);

    public static CookieOptions Create(bool secure) => new()
    {
        HttpOnly = true,
        Secure = secure,
        SameSite = SameSiteMode.Lax,
        Path = "/",
        MaxAge = AccessLifetime,
        IsEssential = true,
    };
    public static CookieOptions CreateRefresh(bool secure) => new()
    {
        HttpOnly = true,
        Secure = secure,
        SameSite = SameSiteMode.Lax,
        Path = "/auth", // not sent to /cases
        MaxAge = RefreshLifetime,
        IsEssential = true,
    };
    public static CookieOptions Delete(bool secure) => new()
    {
        HttpOnly = true,
        Secure = secure,
        SameSite = SameSiteMode.Lax,
        Path = "/",
        IsEssential = true,
    };
    public static CookieOptions DeleteRefresh(bool secure) => new()
    {
        HttpOnly = true,
        Secure = secure,
        SameSite = SameSiteMode.Lax,
        Path = "/auth",
        IsEssential = true,
    };

    public static void ReadJwtFromCookie(JwtBearerOptions options)
    {
        options.Events ??= new JwtBearerEvents();
        var previous = options.Events.OnMessageReceived;
        options.Events.OnMessageReceived = async context =>
        {
            if (previous is not null)
                await previous(context);

            if (string.IsNullOrEmpty(context.Token)
                && context.Request.Cookies.TryGetValue(Name, out var token))
            {
                context.Token = token;
            }
        };
    }
}