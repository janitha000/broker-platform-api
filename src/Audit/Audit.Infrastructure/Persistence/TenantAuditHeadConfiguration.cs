using Audit.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Audit.Infrastructure.Persistence;

public sealed class TenantAuditHeadConfiguration : IEntityTypeConfiguration<TenantAuditHead>
{
    public void Configure(EntityTypeBuilder<TenantAuditHead> builder)
    {
        builder.ToTable("TenantAuditHeads");
        builder.HasKey(x => x.TenantId);
        builder.Property(x => x.LastEventId).IsRequired();
        builder.Property(x => x.LastHash).HasMaxLength(64).IsRequired();
    }
}