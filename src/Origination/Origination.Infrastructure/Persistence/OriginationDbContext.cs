using Microsoft.EntityFrameworkCore;
using Origination.Domain.Cases;
using Origination.Domain.Outbox;

namespace Origination.Infrastructure.Persistence;

public sealed class OriginationDbContext : DbContext
{
    public OriginationDbContext(DbContextOptions<OriginationDbContext> options) : base(options)
    {
    }


    public DbSet<Case> Cases => Set<Case>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CaseConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
    }

}