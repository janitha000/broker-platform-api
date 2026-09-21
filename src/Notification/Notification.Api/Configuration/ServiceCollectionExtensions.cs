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
        services.AddBrokerWebHost(
            configuration,
            new BrokerWebHostOptions { AllowCredentials = true });
        services.AddHttpContextAccessor();
        services.AddBrokerSessionAuthentication(configuration);
        services.AddAuthorization();
        services.AddSignalR();
        services.AddSingleton<IRealtimeNotifier, SignalRRealtimeNotifier>();
        return services;
    }
}
