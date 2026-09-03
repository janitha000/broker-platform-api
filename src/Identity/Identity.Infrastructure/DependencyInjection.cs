using Identity.Application.Abstractions;
using Identity.Domain.Tenants;
using Identity.Infrastructure.Auth;
using Identity.Infrastructure.Payments;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<PaymentOptions>(configuration.GetSection(PaymentOptions.SectionName));
        services.AddSingleton<IPasswordHasher, AspNetPasswordHasher>();
        services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();
        services.AddSingleton<Auth0ManagementTokenCache>();
        services.AddSingleton<Auth0PaymentTokenCache>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IBrokerUserRepository, BrokerUserRepository>();
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Identity")));
        services.AddHttpClient<IPaymentGateway, HttpPaymentGateway>((sp, client) =>
        {
            var baseUrl = sp.GetRequiredService<IOptions<PaymentOptions>>().Value.BaseUrl;
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException("Payment:BaseUrl is not configured.");

            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.Configure<Auth0Options>(configuration.GetSection(Auth0Options.SectionName));
        services.AddHttpClient<IAuth0UserDirectory, HttpAuth0UserDirectory>((sp, client) =>
        {
            var auth0 = sp.GetRequiredService<IOptions<Auth0Options>>().Value;
            client.BaseAddress = new Uri($"https://{auth0.Domain}/");
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddHttpClient<Auth0PaymentTokenProvider>((sp, client) =>
        {
            var auth0 = sp.GetRequiredService<IOptions<Auth0Options>>().Value;
            client.BaseAddress = new Uri($"https://{auth0.Domain}/");
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        return services;
    }
}