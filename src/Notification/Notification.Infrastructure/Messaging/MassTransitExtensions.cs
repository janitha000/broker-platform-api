using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Notification.Infrastructure.Messaging;

public static class MassTransitExtensions
{
    public static void AddCaseFactFindCompletedConsumer(this IBusRegistrationConfigurator bus)
    {
        bus.AddConsumer<CaseFactFindCompletedConsumer>();
        bus.AddConfigureEndpointsCallback((_, cfg) =>
            cfg.UseMessageRetry(NotificationMessageRetry.Configure));
    }

    public static IServiceCollection AddNotificationMassTransit(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration.GetSection(MassTransitOptions.SectionName).Get<MassTransitOptions>()
            ?? new MassTransitOptions();
        services.Configure<MassTransitOptions>(configuration.GetSection(MassTransitOptions.SectionName));

        services.AddMassTransit(bus =>
        {
            bus.AddCaseFactFindCompletedConsumer();
            if (options.UseRabbitMq)
            {
                bus.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(options.Host, options.VirtualHost, h =>
                    {
                        h.Username(options.Username);
                        h.Password(options.Password);
                    });
                    cfg.ConfigureEndpoints(context);
                });
            }
            else
            {
                bus.UsingInMemory((context, cfg) =>
                {
                    cfg.ConfigureEndpoints(context);
                });
            }
        });

        return services;
    }
}
