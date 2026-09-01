namespace Origination.Domain.Abstractions;

public interface IUnitOfWork
{
    Task SaveChanges(CancellationToken cancellationToken = default);
}