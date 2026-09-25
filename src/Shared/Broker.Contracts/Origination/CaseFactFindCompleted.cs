namespace Broker.Contracts.Origination;

/// <summary>
/// Typed bus message for fact-find completion. Origination publishes this from the outbox
/// when MassTransit:Transport is RabbitMq; Notification consumes it.
/// </summary>
public sealed record CaseFactFindCompleted
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
