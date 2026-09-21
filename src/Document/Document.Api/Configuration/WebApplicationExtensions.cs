using Broker.Hosting;

namespace Document.Api.Configuration;

public static class WebApplicationExtensions
{
    public static WebApplication UseDocumentApi(this WebApplication app)
    {
        app.UseBrokerWebHost();
        return app;
    }
}
