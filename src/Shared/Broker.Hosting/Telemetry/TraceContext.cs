using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Broker.Hosting.Telemetry;

public static class TraceContext
{
    public const string TraceParentProperty = "traceparent";
    public const string TraceStateProperty = "tracestate";

    public static void Capture(out string? traceParent, out string? traceState)
    {
        var activity = Activity.Current;
        traceParent = activity?.Id;
        traceState = string.IsNullOrEmpty(activity?.TraceStateString)
            ? null
            : activity.TraceStateString;
    }

    public static string InjectIntoJson(string payload)
    {
        var activity = Activity.Current;
        if (activity?.Id is null)
            return payload;

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(string.IsNullOrWhiteSpace(payload) ? "{}" : payload);
        }
        catch (JsonException)
        {
            return payload;
        }

        if (node is not JsonObject obj)
            return payload;

        obj[TraceParentProperty] = activity.Id;
        if (!string.IsNullOrEmpty(activity.TraceStateString))
            obj[TraceStateProperty] = activity.TraceStateString;
        else
            obj.Remove(TraceStateProperty);

        return obj.ToJsonString();
    }

    public static void TryGetFromJson(JsonElement element, out string? traceParent, out string? traceState)
    {
        traceParent = null;
        traceState = null;
        if (element.ValueKind != JsonValueKind.Object)
            return;

        if (element.TryGetProperty(TraceParentProperty, out var parent))
            traceParent = parent.GetString();
        if (element.TryGetProperty(TraceStateProperty, out var state))
            traceState = state.GetString();
    }

    public static Activity? Start(
        string operationName,
        ActivityKind kind,
        string? traceParent,
        string? traceState)
    {
        if (!string.IsNullOrWhiteSpace(traceParent)
            && ActivityContext.TryParse(traceParent, traceState, out var parent)
            && parent != default)
        {
            return BrokerTelemetry.Source.StartActivity(operationName, kind, parent);
        }

        return BrokerTelemetry.Source.StartActivity(operationName, kind);
    }

    public static async Task Publish(
        string type,
        string payload,
        string? traceParent,
        string? traceState,
        Func<string, string, CancellationToken, Task> publish,
        CancellationToken cancellationToken)
    {
        using var activity = Start($"publish {type}", ActivityKind.Producer, traceParent, traceState);
        activity?.SetTag("messaging.system", "aws.eventbridge");
        activity?.SetTag("messaging.operation", "publish");
        activity?.SetTag("messaging.destination.name", type);
        await publish(type, payload, cancellationToken);
    }
}
