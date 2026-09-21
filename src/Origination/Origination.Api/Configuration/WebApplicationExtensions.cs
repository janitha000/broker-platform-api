using Broker.Hosting;

namespace Origination.Api.Configuration;

public static class WebApplicationExtensions
{
    public static WebApplication UseOriginationApi(this WebApplication app)
    {
        app.UseBrokerWebHost();
        return app;
    }
}