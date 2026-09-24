using System.Diagnostics;
using System.Text.Json;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using Audit.Application.Events.IngestAuditEvent;
using Broker.Hosting.Audit;
using Broker.Hosting.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Audit.Infrastructure.Messaging;

public sealed class AuditQueueWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SqsWorkerOptions _options;
    private readonly ILogger<AuditQueueWorker> _logger;

    public AuditQueueWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<SqsWorkerOptions> options,
        ILogger<AuditQueueWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_options.QueueUrl))
        {
            _logger.LogWarning("Messaging:QueueUrl empty; audit worker not started");
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
                        var envelope = ReadAuditEvent(message.Body);
                        if (envelope is null)
                        {
                            _logger.LogWarning("SQS {MessageId} is not an AuditEvent; dropping", message.MessageId);
                            await sqs.DeleteMessageAsync(_options.QueueUrl, message.ReceiptHandle, stoppingToken);
                            continue;
                        }

                        using var activity = TraceContext.Start(
                            "process AuditEvent",
                            ActivityKind.Consumer,
                            envelope.TraceParent,
                            envelope.TraceState);
                        activity?.SetTag("messaging.system", "aws.sqs");
                        activity?.SetTag("messaging.operation", "process");
                        activity?.SetTag("messaging.message.id", message.MessageId);

                        if (string.IsNullOrWhiteSpace(envelope.Event.TraceId))
                            envelope.Event.TraceId = Activity.Current?.TraceId.ToString();

                        await using var scope = _scopeFactory.CreateAsyncScope();
                        var handler = scope.ServiceProvider.GetRequiredService<IngestAuditEventHandler>();
                        await handler.Handle(envelope.Event, stoppingToken);
                        await sqs.DeleteMessageAsync(_options.QueueUrl, message.ReceiptHandle, stoppingToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogError(ex, "Failed to ingest audit SQS {MessageId}", message.MessageId);
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

    private sealed record ParsedAuditEvent(AuditEvent Event, string? TraceParent, string? TraceState);

    private static ParsedAuditEvent? ReadAuditEvent(string body)
    {
        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;

        if (!root.TryGetProperty("detail-type", out var detailType)
            || detailType.GetString() != AuditEventTypes.AuditEvent)
            return null;

        if (!root.TryGetProperty("detail", out var detail)
            || detail.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            return null;

        TraceContext.TryGetFromJson(detail, out var traceParent, out var traceState);
        var auditEvent = JsonSerializer.Deserialize<AuditEvent>(detail.GetRawText(), AuditEventJson.Options);
        return auditEvent is null ? null : new ParsedAuditEvent(auditEvent, traceParent, traceState);
    }
}
