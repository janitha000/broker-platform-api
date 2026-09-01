using Microsoft.Extensions.DependencyInjection;
using Notification.Application.Abstractions;
using Notification.Application.Notifications.SendNotification;
using Notification.Application.Templating;

namespace Notification.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<SendNotificationHandler>();
        services.AddSingleton<ITemplateRenderer, PlaceholderTemplateRenderer>();
        return services;
    }
}