using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Origination.Application.Abstractions;

namespace Origination.Infrastructure.Persistence;

public sealed class OriginationDbContextFactory : IDesignTimeDbContextFactory<OriginationDbContext>
{
    public OriginationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<OriginationDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\mssqllocaldb;Database=Origination;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new OriginationDbContext(options, new DesignTimeTenantContext());
    }

    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public Guid? TenantId => null;
    }
}
