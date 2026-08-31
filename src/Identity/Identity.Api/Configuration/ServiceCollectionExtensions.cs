using Broker.Hosting;

namespace Identity.Api.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddBrokerJwtAuthentication(configuration);
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
                        .AllowAnyMethod();
                });
            });
        }

        return services;
    }
}
