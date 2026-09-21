namespace Broker.Hosting;

public sealed class BrokerWebHostOptions
{
    /// <summary>
    /// Identity and Notification need this when Cors:Origins is non-empty
    /// (cookies / SignalR). Leave false for Origination-style APIs.
    /// </summary>
    public bool AllowCredentials { get; init; }
}