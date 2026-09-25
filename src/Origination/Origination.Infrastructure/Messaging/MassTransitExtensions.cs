using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Origination.Application.Abstractions;
using Origination.Infrastructure.Messaging.Sagas;
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
        {
            services.AddScoped<ICaseFactFindCompletedPublisher, BusOutboxCaseFactFindCompletedPublisher>();
            services.AddScoped<ICaseOpenedPublisher, BusOutboxCaseOpenedPublisher>();
        }
        else
        {
            services.AddScoped<ICaseFactFindCompletedPublisher, DisabledBusOutboxCaseFactFindCompletedPublisher>();
            services.AddScoped<ICaseOpenedPublisher, DisabledBusOutboxCaseOpenedPublisher>();
        }

        services.AddMassTransit(bus =>
        {
            bus.AddEntityFrameworkOutbox<OriginationDbContext>(outbox =>
            {
                outbox.QueryDelay = TimeSpan.FromSeconds(1);
                outbox.UseSqlServer();
                outbox.UseBusOutbox();
            });

            AddCaseLifecycleSaga(bus, options.UseRabbitMq);

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
                bus.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
            }
        });

        return services;
    }

    public static void AddCaseLifecycleSaga(this IBusRegistrationConfigurator bus, bool persistToSql)
    {
        var saga = bus.AddSagaStateMachine<CaseLifecycleStateMachine, CaseLifecycleState>();
        if (persistToSql)
        {
            saga.EntityFrameworkRepository(r =>
            {
                r.ConcurrencyMode = ConcurrencyMode.Pessimistic;
                r.ExistingDbContext<OriginationDbContext>();
                r.UseSqlServer();
            });
        }
        else
        {
            saga.InMemoryRepository();
        }
    }
}
