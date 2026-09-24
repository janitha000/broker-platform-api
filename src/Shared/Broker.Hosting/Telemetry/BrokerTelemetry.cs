using System.Diagnostics;

namespace Broker.Hosting.Telemetry;

public static class BrokerTelemetry
{
    public const string ActivitySourceName = "Broker.Messaging";

    public static readonly ActivitySource Source = new(ActivitySourceName);
}
