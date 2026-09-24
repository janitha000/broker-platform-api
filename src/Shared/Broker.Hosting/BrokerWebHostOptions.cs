namespace Broker.Hosting;

public sealed class BrokerWebHostOptions
{
    /// <summary>
    /// Identity and Notification need this when Cors:Origins is non-empty
    /// (cookies / SignalR). Leave false for Origination-style APIs.
    /// </summary>
    public bool AllowCredentials { get; init; }

    /// <summary>
    /// OpenTelemetry resource service name (Jaeger/Tempo grouping).
    /// Falls back to Telemetry:ServiceName / OTEL_SERVICE_NAME / broker-api.
    /// </summary>
    public string? ServiceName { get; init; }
}