namespace Identity.Domain.Registration;

public interface IRegistrationSagaRepository
{
    Task<RegistrationSaga?> GetByIdempotencyKey(string key, CancellationToken ct = default);
    Task<RegistrationSaga> Add(RegistrationSaga saga, CancellationToken ct = default);
    Task Update(RegistrationSaga saga, CancellationToken ct = default);
    Task<IReadOnlyList<RegistrationSaga>> GetIncomplete(DateTime olderThan, int take, CancellationToken ct = default);
}
