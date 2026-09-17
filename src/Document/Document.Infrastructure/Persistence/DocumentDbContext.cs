using Document.Application.Abstractions;
using Document.Domain.Abstractions;
using Document.Domain.Documents;
using Document.Domain.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Document.Infrastructure.Persistence;

public sealed class DocumentDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public DocumentDbContext(
        DbContextOptions<DocumentDbContext> options,
        ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<CaseDocument> CaseDocuments => Set<CaseDocument>();
    public DbSet<DocumentAccessLog> DocumentAccessLogs => Set<DocumentAccessLog>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CaseDocumentConfiguration());
        modelBuilder.ApplyConfiguration(new DocumentAccessLogConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());

        modelBuilder.Entity<CaseDocument>()
            .HasQueryFilter(x =>
                _tenantContext.TenantId == null || x.TenantId == _tenantContext.TenantId);

        modelBuilder.Entity<DocumentAccessLog>()
            .HasQueryFilter(x =>
                _tenantContext.TenantId == null || x.TenantId == _tenantContext.TenantId);
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
        var tenantId = _tenantContext.TenantId;

        foreach (var entry in ChangeTracker.Entries()
                     .Where(e => e.Entity is ITenantOwnedEntity
                         && e.State is EntityState.Added
                             or EntityState.Modified
                             or EntityState.Deleted))
        {
            var entity = (ITenantOwnedEntity)entry.Entity;

            if (tenantId is null)
            {
                if (entry.State == EntityState.Added)
                {
                    throw new InvalidOperationException(
                        "Cannot insert tenant-owned data without a tenant context.");
                }

                continue;
            }

            if (entry.State == EntityState.Added && entity.TenantId == Guid.Empty)
                entity.TenantId = tenantId.Value;

            if (entity.TenantId != tenantId.Value)
                throw new InvalidOperationException("Cannot mutate data owned by another tenant.");

            var tenantProperty = entry.Property(nameof(ITenantOwnedEntity.TenantId));
            if (entry.State == EntityState.Modified
                && tenantProperty.IsModified
                && !Equals(tenantProperty.OriginalValue, tenantProperty.CurrentValue))
            {
                throw new InvalidOperationException("TenantId cannot be changed.");
            }
        }
    }
}