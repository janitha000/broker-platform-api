using Microsoft.Extensions.DependencyInjection;
using Notification.Application.Notifications.SendNotification;

namespace Notification.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<SendNotificationHandler>();
        return services;
    }
}
