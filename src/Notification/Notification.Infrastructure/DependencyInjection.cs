using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notification.Application.Abstractions;
using Notification.Domain.Notifications;
using Notification.Infrastructure.Email;
using Notification.Infrastructure.Persistence;

namespace Notification.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.AddDbContext<NotificationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Notification")));
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationTemplateRepository, NotificationTemplateRepository>();

        var provider = configuration["Email:Provider"] ?? "Mock";
        if (string.Equals(provider, "Ses", StringComparison.OrdinalIgnoreCase))
            services.AddSingleton<IEmailProvider, SesEmailProvider>();
        else
            services.AddSingleton<IEmailProvider, MockEmailProvider>();

        return services;
    }
}