using Broker.Hosting;

namespace Payment.Api.Configuration;

public static class WebApplicationExtensions
{
    public static WebApplication UsePaymentApi(this WebApplication app)
    {
        app.UseBrokerWebHost();
        return app;
    }
}
