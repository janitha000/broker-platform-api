using Broker.Hosting;
using Audit.Api.Auth;
using Audit.Application.Abstractions;
using Audit.Application.Auth;

namespace Audit.Api.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuditApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBrokerWebHost(configuration, new BrokerWebHostOptions { ServiceName = "audit-api" });

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentBroker, JwtCurrentBroker>();

        services.AddBrokerSessionAuthentication(configuration);
        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                AuditAuth.ReadPolicy,
                policy => policy.RequireAssertion(ctx =>
                    AuditAuth.HasPermission(ctx.User, AuditPermissions.Read)));
        });
        return services;
    }
}
