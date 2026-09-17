namespace Document.Domain.Abstractions;

public interface IUnitOfWork
{
    Task SaveChanges(CancellationToken cancellationToken = default);
}
