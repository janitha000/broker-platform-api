namespace Payment.Domain.Charges;

public interface IChargeRepository
{
    Task<Charge?> GetByIdempotencyKey(string idempotencyKey, CancellationToken cancellationToken = default);
    Task<Charge?> GetById(Guid id, CancellationToken cancellationToken = default);
    Task<Charge> Add(Charge charge, CancellationToken cancellationToken = default);
    Task Update(Charge charge, CancellationToken cancellationToken = default);
}
