using System.Text.Json;
using System.Text.Json.Serialization;

namespace Broker.Hosting.Audit;

public static class AuditEventJson
{
    public const int MaxDataJsonChars = 1500;
    public const int MaxUserAgentChars = 256;

    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Serialize(AuditEvent auditEvent)
    {
        var data = auditEvent.DataJson;
        if (data is { Length: > MaxDataJsonChars })
            data = data[..MaxDataJsonChars];

        var ua = auditEvent.Actor.UserAgent;
        if (ua is { Length: > MaxUserAgentChars })
            ua = ua[..MaxUserAgentChars];

        var payload = new AuditEvent
        {
            EventId = auditEvent.EventId == Guid.Empty ? Guid.NewGuid() : auditEvent.EventId,
            OccurredAt = auditEvent.OccurredAt == default ? DateTime.UtcNow : auditEvent.OccurredAt,
            TenantId = auditEvent.TenantId,
            Action = auditEvent.Action,
            Outcome = auditEvent.Outcome,
            CorrelationId = auditEvent.CorrelationId,
            RequestId = auditEvent.RequestId,
            DataJson = data,
            Actor = new AuditActor
            {
                Type = auditEvent.Actor.Type,
                BrokerId = auditEvent.Actor.BrokerId,
                Subject = auditEvent.Actor.Subject,
                IpAddress = auditEvent.Actor.IpAddress,
                UserAgent = ua,
            },
            Resource = auditEvent.Resource,
        };

        return JsonSerializer.Serialize(payload, Options);
    }
}