using Broker.Hosting;
using Origination.Api.Auth;
using Origination.Application.Abstractions;
using Origination.Application.Auth;

namespace Origination.Api.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOriginationApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBrokerWebHost(configuration);

        services.AddHttpContextAccessor();
        services.AddScoped<JwtCurrentBroker>();
        services.AddScoped<ICurrentBroker>(sp => sp.GetRequiredService<JwtCurrentBroker>());
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<JwtCurrentBroker>());

        services.AddBrokerSessionAuthentication(configuration);
        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                OriginationAuth.ReadPolicy,
                policy => policy.RequireAssertion(ctx =>
                    OriginationAuth.HasPermission(ctx.User, CasePermissions.Read)));
            options.AddPolicy(
                OriginationAuth.CreatePolicy,
                policy => policy.RequireAssertion(ctx =>
                    OriginationAuth.HasPermission(ctx.User, CasePermissions.Create)));
            options.AddPolicy(
                OriginationAuth.FactFindPolicy,
                policy => policy.RequireAssertion(ctx =>
                    OriginationAuth.HasPermission(ctx.User, CasePermissions.FactFind)));
            options.AddPolicy(
                OriginationAuth.LodgePolicy,
                policy => policy.RequireAssertion(ctx =>
                    OriginationAuth.HasPermission(ctx.User, CasePermissions.Lodge)));
            options.AddPolicy(
                OriginationAuth.SettlePolicy,
                policy => policy.RequireAssertion(ctx =>
                    OriginationAuth.HasPermission(ctx.User, CasePermissions.Settle)));
        });

        return services;
    }

}
