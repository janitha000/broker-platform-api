using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        services.AddMassTransit(bus =>
        {
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
