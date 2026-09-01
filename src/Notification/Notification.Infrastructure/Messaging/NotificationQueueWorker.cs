using System.Text.Json;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notification.Application.Notifications.SendNotification;

namespace Notification.Infrastructure.Messaging;

public sealed class NotificationQueueWorker : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SqsWorkerOptions _options;
    private readonly ILogger<NotificationQueueWorker> _logger;

    public NotificationQueueWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<SqsWorkerOptions> options,
        ILogger<NotificationQueueWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_options.QueueUrl))
        {
            _logger.LogInformation("Messaging:QueueUrl empty; SQS worker not started");
            return;
        }

        using var sqs = new AmazonSQSClient(RegionEndpoint.GetBySystemName(_options.AwsRegion));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var response = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = _options.QueueUrl,
                    MaxNumberOfMessages = 5,
                    WaitTimeSeconds = 20,
                    VisibilityTimeout = 60,
                }, stoppingToken);

                foreach (var message in response.Messages ?? [])
                {
                    try
                    {
                        await HandleMessage(sqs, message, stoppingToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogError(ex, "Failed to process SQS message {MessageId}", message.MessageId);
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "SQS receive failed");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task HandleMessage(IAmazonSQS sqs, Message message, CancellationToken cancellationToken)
    {
        var envelope = JsonSerializer.Deserialize<EventBridgeSqsEnvelope>(message.Body, JsonOptions);
        var detail = envelope?.Detail;
        if (detail is null || string.IsNullOrWhiteSpace(detail.IdempotencyKey))
            throw new InvalidOperationException("SQS body is not a CaseFactFindCompleted EventBridge event.");

        await using var scope = _scopeFactory.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<SendNotificationHandler>();

        var outcome = await handler.Handle(
            new SendNotificationCommand(
                string.IsNullOrWhiteSpace(detail.Channel) ? "Email" : detail.Channel,
                detail.Recipient ?? $"broker-{detail.BrokerId}@invalid.local",
                detail.TemplateKey,
                detail.Data,
                "origination",
                detail.IdempotencyKey,
                detail.CorrelationId),
            cancellationToken);

        if (outcome.Kind is SendNotificationKind.TemplateNotFound)
            throw new InvalidOperationException($"Unknown template {detail.TemplateKey}");

        // Sent, Failed (provider), Conflict (already processed) → delete
        await sqs.DeleteMessageAsync(_options.QueueUrl, message.ReceiptHandle, cancellationToken);
    }
}