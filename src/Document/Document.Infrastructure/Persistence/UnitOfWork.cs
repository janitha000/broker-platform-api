using Document.Domain.Abstractions;

namespace Document.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly DocumentDbContext _context;

    public UnitOfWork(DocumentDbContext context)
    {
        _context = context;
    }

    public Task SaveChanges(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
