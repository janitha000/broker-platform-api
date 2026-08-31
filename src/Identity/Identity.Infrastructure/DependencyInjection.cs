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
        return services;
    }
}