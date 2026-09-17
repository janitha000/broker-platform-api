using Microsoft.EntityFrameworkCore;

namespace Document.Infrastructure.Persistence;

public sealed class DocumentDbContext : DbContext
{
    public DocumentDbContext(DbContextOptions<DocumentDbContext> options)
        : base(options)
    {
    }
}
