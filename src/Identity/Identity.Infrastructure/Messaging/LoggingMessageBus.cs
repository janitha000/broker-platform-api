using Broker.Hosting.Telemetry;
using Identity.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Messaging;

public sealed class LoggingMessageBus : IMessageBus
{
    private readonly ILogger<LoggingMessageBus> _logger;

    public LoggingMessageBus(ILogger<LoggingMessageBus> logger)
    {
        _logger = logger;
    }

    public Task Publish(string type, string payload, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Outbox publish {Type} {Payload}", type, TraceContext.InjectIntoJson(payload));
        return Task.CompletedTask;
    }
}
