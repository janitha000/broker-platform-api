using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Origination.Application.Abstractions;
using Origination.Infrastructure.Persistence;

namespace Origination.Infrastructure.Messaging;

public static class MassTransitExtensions
{
    public static IServiceCollection AddOriginationMassTransit(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration.GetSection(MassTransitOptions.SectionName).Get<MassTransitOptions>()
            ?? new MassTransitOptions();
        services.Configure<MassTransitOptions>(configuration.GetSection(MassTransitOptions.SectionName));

        if (options.UseRabbitMq)
            services.AddScoped<ICaseFactFindCompletedPublisher, BusOutboxCaseFactFindCompletedPublisher>();
        else
            services.AddScoped<ICaseFactFindCompletedPublisher, DisabledBusOutboxCaseFactFindCompletedPublisher>();

        services.AddMassTransit(bus =>
        {
            bus.AddEntityFrameworkOutbox<OriginationDbContext>(outbox =>
            {
                outbox.QueryDelay = TimeSpan.FromSeconds(1);
                outbox.UseSqlServer();
                outbox.UseBusOutbox();
            });

            if (options.UseRabbitMq)
            {
                bus.UsingRabbitMq((_, cfg) =>
                {
                    cfg.Host(options.Host, options.VirtualHost, h =>
                    {
                        h.Username(options.Username);
                        h.Password(options.Password);
                    });
                });
            }
            else
            {
                bus.UsingInMemory((_, cfg) => { });
            }
        });

        return services;
    }
}
