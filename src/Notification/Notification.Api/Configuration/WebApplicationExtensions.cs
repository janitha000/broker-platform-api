using Broker.Hosting;
using Notification.Api.Realtime;

namespace Notification.Api.Configuration;

public static class WebApplicationExtensions
{
    public static WebApplication UseNotificationApi(this WebApplication app)
    {
        app.UseBrokerWebHost();
        app.MapHub<NotificationsHub>("/hubs/notifications");
        return app;
    }
}
