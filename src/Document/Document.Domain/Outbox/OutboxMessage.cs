namespace Document.Domain.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? TraceParent { get; set; }
    public string? TraceState { get; set; }
}

public interface IOutbox
{
    Task<bool> Exists(string idempotencyKey, CancellationToken cancellationToken = default);
    void Add(OutboxMessage message);
}