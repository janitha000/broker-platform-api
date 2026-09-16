using Broker.Hosting;
using Broker.Hosting.Auth;
using Identity.Api.Auth;
using Identity.Application.Abstractions;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Identity.Api.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddBrokerSessionAuthentication(configuration);
        services.AddAuth0Authentication(configuration);
        services.AddAuthorization();
        services.AddControllers();
        services.AddCorsFromConfiguration(configuration);
        return services;
    }

    private static IServiceCollection AddCorsFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var corsOrigins = configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
        if (corsOrigins.Length > 0)
        {
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins(corsOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });
        }

        return services;
    }

    private static IServiceCollection AddAuth0Authentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        var auth0 = configuration.GetSection(Auth0Options.SectionName).Get<Auth0Options>()
            ?? throw new InvalidOperationException("Auth0 is not configured.");
        if (string.IsNullOrWhiteSpace(auth0.Domain)
            || string.IsNullOrWhiteSpace(auth0.ClientId)
            || string.IsNullOrWhiteSpace(auth0.ClientSecret)
            || string.IsNullOrWhiteSpace(auth0.AppBaseUrl)
            || string.IsNullOrWhiteSpace(auth0.Audience)
            || string.IsNullOrWhiteSpace(auth0.ManagementClientId)
            || string.IsNullOrWhiteSpace(auth0.ManagementClientSecret)
            || string.IsNullOrWhiteSpace(auth0.PaymentAudience)
            || string.IsNullOrWhiteSpace(auth0.PaymentClientId)
            || string.IsNullOrWhiteSpace(auth0.PaymentClientSecret))
            throw new InvalidOperationException(
                "Auth0:Domain, Audience, ClientId, ClientSecret, AppBaseUrl, ManagementClientId, ManagementClientSecret, PaymentAudience, PaymentClientId, and PaymentClientSecret are required.");

        var useOrganizations = AuthModes.UseAuth0Organizations(configuration);

        services.AddAuthentication()
            .AddCookie(Auth0Auth.CookieScheme, options =>
            {
                options.Cookie.Name = "broker.oidc";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            })
            .AddOpenIdConnect(Auth0Auth.ChallengeScheme, options =>
            {
                options.Authority = $"https://{auth0.Domain}";
                options.ClientId = auth0.ClientId;
                options.ClientSecret = auth0.ClientSecret;
                options.ResponseType = "code";
                options.CallbackPath = "/auth/callback";
                options.SignInScheme = Auth0Auth.CookieScheme;
                options.SaveTokens = useOrganizations;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                if (useOrganizations)
                    options.Scope.Add("offline_access");
                options.CorrelationCookie.SameSite = SameSiteMode.Lax;
                options.NonceCookie.SameSite = SameSiteMode.Lax;
                options.Events = new OpenIdConnectEvents
                {
                    OnRedirectToIdentityProvider = context =>
                    {
                        // Browser origin, not the Kestrel host (5250 / 8080).
                        context.ProtocolMessage.RedirectUri =
                            $"{auth0.AppBaseUrl.TrimEnd('/')}/auth/callback";
                        context.ProtocolMessage.SetParameter("audience", auth0.Audience);
                        if (useOrganizations
                            && context.Properties.Items.TryGetValue("organization", out var organization)
                            && !string.IsNullOrWhiteSpace(organization))
                        {
                            context.ProtocolMessage.SetParameter("organization", organization);
                        }

                        return Task.CompletedTask;
                    },
                };
            });
        return services;
    }
}
