using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Notification.Infrastructure.Messaging;

public static class MassTransitExtensions
{
    public static IServiceCollection AddNotificationMassTransit(this IServiceCollection services)
    {
        services.AddMassTransit(bus =>
        {
            bus.AddConsumer<CaseFactFindCompletedConsumer>();
            bus.UsingInMemory((context, cfg) =>
            {
                cfg.ConfigureEndpoints(context);
            });
        });
        return services;
    }
}
