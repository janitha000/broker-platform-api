using Broker.Hosting;

namespace Identity.Api.Configuration;

public static class WebApplicationExtensions
{
    public static WebApplication UseIdentityApi(this WebApplication app)
    {
        app.UseBrokerWebHost();
        return app;
    }
}
