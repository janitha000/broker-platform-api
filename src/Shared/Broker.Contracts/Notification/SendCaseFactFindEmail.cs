namespace Broker.Contracts.Notification;

/// <summary>
/// Command from the Origination case-lifecycle saga to Notification.
/// Delivered with <c>Send</c> to <see cref="BrokerCommandQueues.SendCaseFactFindEmail"/>,
/// not published for multiple subscribers.
/// </summary>
public sealed record SendCaseFactFindEmail
{
    public Guid CaseId { get; init; }
    public Guid TenantId { get; init; }
    public Guid BrokerId { get; init; }
    public string TemplateKey { get; init; } = "case.fact-find-completed";
    public string Channel { get; init; } = "Email";
    public Dictionary<string, string>? Data { get; init; }
    public string IdempotencyKey { get; init; } = string.Empty;
    public string? CorrelationId { get; init; }
    public string? Recipient { get; init; }
}
