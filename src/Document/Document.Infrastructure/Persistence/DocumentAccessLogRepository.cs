using Document.Domain.Documents;

namespace Document.Infrastructure.Persistence;

public sealed class DocumentAccessLogRepository : IDocumentAccessLogRepository
{
    private readonly DocumentDbContext _context;

    public DocumentAccessLogRepository(DocumentDbContext context)
    {
        _context = context;
    }

    public Task Add(DocumentAccessLog log, CancellationToken cancellationToken = default)
    {
        _context.DocumentAccessLogs.Add(log);
        return Task.CompletedTask;
    }
}
