using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notification.Application.Notifications.SendNotification;
using Notification.Domain.Inbox;
using Notification.Infrastructure.Persistence;

namespace Notification.Infrastructure.Messaging;

public sealed class InboxDispatcher : BackgroundService
{
    private const int MaxAttempts = 8;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InboxDispatcher> _logger;

    public InboxDispatcher(IServiceScopeFactory scopeFactory, ILogger<InboxDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatch(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Inbox dispatch batch failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessBatch(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        var handler = scope.ServiceProvider.GetRequiredService<SendNotificationHandler>();
        var now = DateTime.UtcNow;

        var batch = await db.InboxMessages
            .Where(m => m.Status == InboxStatus.Received
                        && m.NextAttemptAt <= now
                        && (m.LockedUntil == null || m.LockedUntil < now))
            .OrderBy(m => m.ReceivedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var message in batch)
            message.LockedUntil = now.AddMinutes(2);
        if (batch.Count > 0)
            await db.SaveChangesAsync(cancellationToken);

        foreach (var message in batch)
        {
            try
            {
                var command = JsonSerializer.Deserialize<SendNotificationCommand>(message.Payload, JsonOptions)
                    ?? throw new InvalidOperationException("Inbox payload is not a SendNotificationCommand.");

                var outcome = await handler.Handle(command, cancellationToken);
                message.AttemptCount++;
                message.LockedUntil = null;

                if (outcome.Kind is SendNotificationKind.Sent or SendNotificationKind.IdempotencyConflict)
                {
                    message.Status = InboxStatus.Processed;
                    message.ProcessedAt = DateTime.UtcNow;
                    message.LastError = null;
                }
                else if (outcome.Kind is SendNotificationKind.TemplateNotFound
                         || message.AttemptCount >= MaxAttempts)
                {
                    message.Status = InboxStatus.Dead;
                    message.LastError = outcome.Kind.ToString();
                }
                else
                {
                    // Failed send — stay Received, backoff, do not ack anything on SQS
                    var delaySeconds = Math.Min(3600, (int)Math.Pow(2, message.AttemptCount) * 5);
                    message.NextAttemptAt = DateTime.UtcNow.AddSeconds(delaySeconds);
                    message.LastError = outcome.Kind.ToString();
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                message.AttemptCount++;
                message.LockedUntil = null;
                message.LastError = ex.Message;
                if (message.AttemptCount >= MaxAttempts)
                    message.Status = InboxStatus.Dead;
                else
                    message.NextAttemptAt = DateTime.UtcNow.AddSeconds(Math.Min(3600, (int)Math.Pow(2, message.AttemptCount) * 5));
                _logger.LogError(ex, "Inbox message {Id} failed", message.Id);
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}