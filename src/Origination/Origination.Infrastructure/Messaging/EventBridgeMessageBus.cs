using Amazon;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Microsoft.Extensions.Options;
using Origination.Application.Abstractions;

namespace Origination.Infrastructure.Messaging;

public sealed class EventBridgeMessageBus : IMessageBus
{
    private readonly MessagingOptions _options;

    public EventBridgeMessageBus(IOptions<MessagingOptions> options)
    {
        _options = options.Value;
    }

    public async Task Publish(string type, string payload, CancellationToken cancellationToken = default)
    {
        using var client = new AmazonEventBridgeClient(RegionEndpoint.GetBySystemName(_options.AwsRegion));
        var response = await client.PutEventsAsync(new PutEventsRequest
        {
            Entries =
            [
                new PutEventsRequestEntry
                {
                    EventBusName = _options.EventBusName,
                    Source = _options.Source,
                    DetailType = type,
                    Detail = payload,
                    Time = DateTime.UtcNow,
                },
            ],
        }, cancellationToken);

        if (response.FailedEntryCount > 0)
        {
            var reason = response.Entries.FirstOrDefault(e => !string.IsNullOrEmpty(e.ErrorCode));
            throw new InvalidOperationException(
                $"EventBridge PutEvents failed: {reason?.ErrorCode} {reason?.ErrorMessage}");
        }
    }
}