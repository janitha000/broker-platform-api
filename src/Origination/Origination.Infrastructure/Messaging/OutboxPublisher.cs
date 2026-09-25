using System.Diagnostics;
using Broker.Hosting.Telemetry;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Origination.Application.Abstractions;
using Origination.Domain.Outbox;
using Origination.Infrastructure.Persistence;

namespace Origination.Infrastructure.Messaging;

public sealed class OutboxPublisher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MassTransitOptions _massTransit;
    private readonly ILogger<OutboxPublisher> _logger;

    public OutboxPublisher(
        IServiceScopeFactory scopeFactory,
        IOptions<MassTransitOptions> massTransit,
        ILogger<OutboxPublisher> logger)
    {
        _scopeFactory = scopeFactory;
        _massTransit = massTransit.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishBatch(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Outbox publish batch failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task PublishBatch(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OriginationDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var batch = await db.OutboxMessages
            .Where(m => m.PublishedAt == null)
            .OrderBy(m => m.OccurredAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var message in batch)
        {
            if (_massTransit.UseRabbitMq
                && message.Type == OutboxMessageTypes.CaseFactFindCompleted)
            {
                using var activity = TraceContext.Start(
                    $"publish {message.Type}",
                    ActivityKind.Producer,
                    message.TraceParent,
                    message.TraceState);
                activity?.SetTag("messaging.system", "rabbitmq");
                activity?.SetTag("messaging.operation", "publish");
                activity?.SetTag("messaging.destination.name", message.Type);

                await publishEndpoint.Publish(
                    CaseFactFindCompletedMapping.Parse(message.Payload),
                    cancellationToken);
            }
            else
            {
                await TraceContext.Publish(
                    message.Type,
                    message.Payload,
                    message.TraceParent,
                    message.TraceState,
                    bus.Publish,
                    cancellationToken);
            }

            message.PublishedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
