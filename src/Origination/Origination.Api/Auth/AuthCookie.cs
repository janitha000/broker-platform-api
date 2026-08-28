using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Origination.Api.Auth;

public static class AuthCookie
{
    public const string Name = "broker.access";

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
