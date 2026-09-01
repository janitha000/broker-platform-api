using Origination.Domain.Abstractions;

namespace Origination.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly OriginationDbContext _context;

    public UnitOfWork(OriginationDbContext context)
    {
        _context = context;
    }

    public Task SaveChanges(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}