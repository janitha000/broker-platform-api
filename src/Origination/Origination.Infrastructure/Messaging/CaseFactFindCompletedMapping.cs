using System.Text.Json;
using Broker.Contracts.Origination;

namespace Origination.Infrastructure.Messaging;

public static class CaseFactFindCompletedMapping
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static CaseFactFindCompleted Parse(string payload)
    {
        var message = JsonSerializer.Deserialize<CaseFactFindCompleted>(payload, JsonOptions);
        if (message is null || string.IsNullOrWhiteSpace(message.IdempotencyKey))
            throw new InvalidOperationException("Outbox payload is not CaseFactFindCompleted.");
        return message;
    }
}
