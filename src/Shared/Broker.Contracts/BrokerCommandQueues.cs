namespace Broker.Contracts;

/// <summary>
/// Queue names for Send (command) destinations. MassTransit-free: both services
/// build <c>queue:{name}</c> URIs themselves.
/// </summary>
public static class BrokerCommandQueues
{
    public const string SendCaseFactFindEmail = "send-case-fact-find-email";
}
