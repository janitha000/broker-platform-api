namespace Origination.Domain.Cases;

public interface ICaseRepository
{
    Task<Case?> GetById(Guid caseId, CancellationToken cancellationToken = default);
    Task Add(Case @case, CancellationToken cancellationToken = default);

}