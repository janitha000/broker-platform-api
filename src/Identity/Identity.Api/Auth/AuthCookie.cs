using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Identity.Api.Auth;

public static class AuthCookie
{
    public const string Name = "broker.access";
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(8);

    public static CookieOptions Create(bool secure) => new()
    {
        HttpOnly = true,
        Secure = secure,
        SameSite = SameSiteMode.Lax,
        Path = "/",
        MaxAge = Lifetime,
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
