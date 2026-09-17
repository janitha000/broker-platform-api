using Audit.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace Audit.Infrastructure.Persistence;

public sealed class AuditDbContext : DbContext
{
    public AuditDbContext(DbContextOptions<AuditDbContext> options)
        : base(options)
    {
    }

    public DbSet<AuditEventRecord> AuditEvents => Set<AuditEventRecord>();
    public DbSet<TenantAuditHead> TenantAuditHeads => Set<TenantAuditHead>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AuditEventRecordConfiguration());
        modelBuilder.ApplyConfiguration(new TenantAuditHeadConfiguration());
    }
}