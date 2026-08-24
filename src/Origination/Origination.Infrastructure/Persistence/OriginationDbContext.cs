using Microsoft.EntityFrameworkCore;
using Origination.Domain.Cases;

namespace Origination.Infrastructure.Persistence;

public sealed class OriginationDbContext : DbContext
{
    public OriginationDbContext(DbContextOptions<OriginationDbContext> options) : base(options)
    {
    }

    public DbSet<Case> Cases => Set<Case>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CaseConfiguration());
    }

}