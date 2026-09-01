using Origination.Application.Abstractions;
using Origination.Domain.Abstractions;
using Origination.Domain.Cases;
using Origination.Domain.Outbox;
using Origination.Infrastructure.Messaging;
using Origination.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Origination.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<ICaseRepository, CaseRepository>();
        services.AddScoped<IOutbox, Outbox>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.Configure<MessagingOptions>(configuration.GetSection(MessagingOptions.SectionName));

        var provider = configuration["Messaging:Provider"] ?? "Logging";
        if (string.Equals(provider, "EventBridge", StringComparison.OrdinalIgnoreCase))
            services.AddSingleton<IMessageBus, EventBridgeMessageBus>();
        else
            services.AddSingleton<IMessageBus, LoggingMessageBus>();

        services.AddHostedService<OutboxPublisher>();
        services.AddDbContext<OriginationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Origination")));
        return services;
    }
}
