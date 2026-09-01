using System.Text.Json.Serialization;

namespace Notification.Infrastructure.Messaging;

public sealed class EventBridgeSqsEnvelope
{
    [JsonPropertyName("detail-type")]
    public string? DetailType { get; set; }

    [JsonPropertyName("detail")]
    public CaseFactFindCompletedDetail? Detail { get; set; }
}

public sealed class CaseFactFindCompletedDetail
{
    public Guid CaseId { get; set; }
    public Guid TenantId { get; set; }
    public Guid BrokerId { get; set; }
    public string TemplateKey { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public Dictionary<string, string>? Data { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public string? Recipient { get; set; }
}