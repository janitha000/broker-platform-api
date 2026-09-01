using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notification.Application.Abstractions;
using Notification.Domain.Notifications;
using Notification.Infrastructure.Persistence;
using Notification.Infrastructure.Senders;

namespace Notification.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<INotificationRepository, InMemoryNotificationRepository>();
        services.AddSingleton<INotificationSender, MockNotificationSender>();
        return services;
    }
}
