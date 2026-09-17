using System.Text.Json;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using Document.Application.Documents.ScanLandedObject;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Document.Infrastructure.Messaging;

public sealed class LandingObjectCreatedWorker : BackgroundService
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SqsWorkerOptions _options;
    private readonly ILogger<LandingObjectCreatedWorker> _logger;

    public LandingObjectCreatedWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<SqsWorkerOptions> options,
        ILogger<LandingObjectCreatedWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_options.QueueUrl))
        {
            _logger.LogWarning("Messaging:QueueUrl empty; landing worker not started");
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
                        var key = ReadLandingKey(message.Body);
                        if (string.IsNullOrWhiteSpace(key))
                        {
                            _logger.LogWarning("SQS {MessageId} had no S3 object key; dropping", message.MessageId);
                            await sqs.DeleteMessageAsync(_options.QueueUrl, message.ReceiptHandle, stoppingToken);
                            continue;
                        }

                        await using var scope = _scopeFactory.CreateAsyncScope();
                        var handler = scope.ServiceProvider.GetRequiredService<ScanLandedObjectHandler>();
                        await handler.Handle(Uri.UnescapeDataString(key.Replace("+", " ")), stoppingToken);
                        await sqs.DeleteMessageAsync(_options.QueueUrl, message.ReceiptHandle, stoppingToken);
                    }
                    catch (DocumentNotReadyException ex)
                    {
                        _logger.LogInformation(ex, "Landing object not ready; SQS will retry");
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogError(ex, "Failed to process landing object {MessageId}", message.MessageId);
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

    private static string? ReadLandingKey(string body)
    {
        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;

        if (root.TryGetProperty("detail", out var detail)
            && detail.TryGetProperty("object", out var evObject)
            && evObject.TryGetProperty("key", out var evKey))
            return evKey.GetString();

        if (root.TryGetProperty("Records", out var records)
            && records.ValueKind == JsonValueKind.Array
            && records.GetArrayLength() > 0)
        {
            return records[0]
                .GetProperty("s3")
                .GetProperty("object")
                .GetProperty("key")
                .GetString();
        }

        return null;
    }
}