using Identity.Application.Tenants.CompleteAuth0Login;
using Identity.Application.Tenants.Login;
using Identity.Application.Tenants.RegisterTenant;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<RegisterTenantHandler>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<CompleteAuth0LoginHandler>();
        return services;
    }
}
