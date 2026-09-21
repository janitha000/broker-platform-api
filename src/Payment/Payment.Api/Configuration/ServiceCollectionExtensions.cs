using Broker.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Payment.Api.Auth;

namespace Payment.Api.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPaymentApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBrokerWebHost(configuration);
        services.AddPaymentAuth0(configuration);
        return services;
    }

    private static IServiceCollection AddPaymentAuth0(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<Auth0Options>(configuration.GetSection(Auth0Options.SectionName));
        var auth0 = configuration.GetSection(Auth0Options.SectionName).Get<Auth0Options>()
            ?? throw new InvalidOperationException("Auth0 is not configured.");
        if (string.IsNullOrWhiteSpace(auth0.Domain)
            || string.IsNullOrWhiteSpace(auth0.Audience))
            throw new InvalidOperationException("Auth0:Domain and Auth0:Audience are required.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = $"https://{auth0.Domain}/";
                options.Audience = auth0.Audience;
                options.MapInboundClaims = false;
                options.TokenValidationParameters.ValidateIssuer = true;
                options.TokenValidationParameters.ValidateAudience = true;
                options.TokenValidationParameters.ValidateLifetime = true;
                options.TokenValidationParameters.ValidIssuer = $"https://{auth0.Domain}/";
                options.TokenValidationParameters.ValidAudience = auth0.Audience;
            });
        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                PaymentAuth.ChargePolicy,
                policy => policy.RequireAssertion(ctx => PaymentAuth.HasChargePermission(ctx.User)));
            options.AddPolicy(
                PaymentAuth.RefundPolicy,
                policy => policy.RequireAssertion(ctx => PaymentAuth.HasRefundPermission(ctx.User)));
        });
        return services;
    }
}
