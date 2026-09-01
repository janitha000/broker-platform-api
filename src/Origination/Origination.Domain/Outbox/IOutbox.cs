namespace Origination.Domain.Outbox;

public interface IOutbox
{
    Task<bool> Exists(string idempotencyKey, CancellationToken cancellationToken = default);
    void Add(OutboxMessage message);
}