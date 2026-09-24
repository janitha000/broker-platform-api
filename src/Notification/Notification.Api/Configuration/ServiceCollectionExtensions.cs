using Broker.Hosting;
using Notification.Api.Realtime;
using Notification.Application.Abstractions;
using StackExchange.Redis;

namespace Notification.Api.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddBrokerWebHost(
            configuration,
            new BrokerWebHostOptions { AllowCredentials = true, ServiceName = "notification-api" });
        services.AddHttpContextAccessor();
        services.AddBrokerSessionAuthentication(configuration);
        services.AddAuthorization();
        services.AddSignalRWithBackplane(configuration);
        return services;
    }

    public static IServiceCollection AddSignalRWithBackplane(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var signalR = services.AddSignalR();
        var redis = configuration.GetConnectionString("SignalR");
        if (!string.IsNullOrWhiteSpace(redis))
        {
            signalR.AddStackExchangeRedis(redis, options =>
            {
                options.Configuration.ChannelPrefix = RedisChannel.Literal("broker-signalr");
                options.Configuration.AbortOnConnectFail = false;
            });
        }

        services.AddSingleton<IRealtimeNotifier, SignalRRealtimeNotifier>();
        return services;
    }
}
