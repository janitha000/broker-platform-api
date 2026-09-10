using Broker.Hosting;
using Notification.Api.Realtime;
using Notification.Application.Abstractions;

namespace Notification.Api.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddControllers();
        services.AddCorsFromConfiguration(configuration);
        services.AddHttpContextAccessor();
        services.AddBrokerJwtAuthentication(configuration);
        services.AddAuthorization();
        services.AddSignalR();
        services.AddSingleton<IRealtimeNotifier, SignalRRealtimeNotifier>();
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
}
