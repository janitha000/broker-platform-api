using Broker.Hosting;

namespace Audit.Api.Configuration;

public static class WebApplicationExtensions
{
    public static WebApplication UseAuditApi(this WebApplication app)
    {
        app.UseBrokerWebHost();
        return app;
    }
}
