using Document.Application.Abstractions;
using Document.Application.Documents;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;

namespace Document.Infrastructure.Persistence;

public sealed class DocumentDbContextFactory : IDesignTimeDbContextFactory<DocumentDbContext>
{
    public DocumentDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<DocumentDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\mssqllocaldb;Database=Document;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new DocumentDbContext(options, new DesignTimeTenantContext());
    }

    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public Guid? TenantId => null;
    }
}
