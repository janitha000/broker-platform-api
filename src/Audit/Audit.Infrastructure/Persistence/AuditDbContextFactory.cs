using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Audit.Infrastructure.Persistence;

public sealed class AuditDbContextFactory : IDesignTimeDbContextFactory<AuditDbContext>
{
    public AuditDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AuditDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\mssqllocaldb;Database=Audit;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new AuditDbContext(options);
    }
}
