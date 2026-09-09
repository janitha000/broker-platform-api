using Microsoft.EntityFrameworkCore;
using Origination.Application.Abstractions;
using Origination.Domain.Cases;
using Origination.Domain.Outbox;

namespace Origination.Infrastructure.Persistence;

public sealed class OriginationDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public OriginationDbContext(
        DbContextOptions<OriginationDbContext> options,
        ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    internal Guid CurrentTenantId => _tenantContext.TenantId ?? Guid.Empty;

    public DbSet<Case> Cases => Set<Case>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CaseConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.Entity<Case>()
            .HasQueryFilter(c => c.TenantId == CurrentTenantId);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyTenantWriteGuards();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyTenantWriteGuards();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyTenantWriteGuards()
    {
        foreach (var entry in ChangeTracker.Entries<Case>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            var tenantId = _tenantContext.TenantId
                ?? throw new InvalidOperationException(
                    "Cannot mutate tenant-owned data without a tenant context.");

            if (entry.State == EntityState.Added && entry.Entity.TenantId == Guid.Empty)
                entry.Entity.TenantId = tenantId;

            if (entry.Entity.TenantId != tenantId)
                throw new InvalidOperationException(
                    "Cannot mutate data owned by another tenant.");

            var tenantProperty = entry.Property(c => c.TenantId);
            if (entry.State == EntityState.Modified
                && tenantProperty.IsModified
                && tenantProperty.OriginalValue != tenantProperty.CurrentValue)
            {
                throw new InvalidOperationException("TenantId cannot be changed.");
            }
        }
    }

    // protected override void OnModelCreating(ModelBuilder modelBuilder)
    // {
    //     base.OnModelCreating(modelBuilder);

    //     // Apply tenant query filter automatically to all multi-tenant entities
    //     modelBuilder.Entity<Lead>()
    //         .HasQueryFilter(e => e.TenantId == _tenantAccessor.CurrentTenant.Id);

    //     modelBuilder.Entity<FactFindDraft>()
    //         .HasQueryFilter(e => e.TenantId == _tenantAccessor.CurrentTenant.Id);
    // }

    // public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    // {
    //     // Automatically inject TenantId on new records
    //     foreach (var entry in ChangeTracker.Entries<IMultiTenantEntity>())
    //     {
    //         if (entry.State == EntityState.Added)
    //         {
    //             entry.Entity.TenantId = _tenantAccessor.CurrentTenant.Id;
    //         }
    //     }
    //     return base.SaveChangesAsync(cancellationToken);
    // }
}
