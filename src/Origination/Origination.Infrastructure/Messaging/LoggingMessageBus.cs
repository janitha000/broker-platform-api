using Microsoft.Extensions.Logging;
using Origination.Application.Abstractions;

namespace Origination.Infrastructure.Messaging;

public sealed class LoggingMessageBus : IMessageBus
{
    private readonly ILogger<LoggingMessageBus> _logger;

    public LoggingMessageBus(ILogger<LoggingMessageBus> logger)
    {
        _logger = logger;
    }

    public Task Publish(string type, string payload, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Outbox publish {Type} {Payload}", type, payload);
        return Task.CompletedTask;
    }
}