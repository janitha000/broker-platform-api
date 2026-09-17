using Audit.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Audit.Infrastructure.Persistence;

public sealed class AuditEventRecordConfiguration : IEntityTypeConfiguration<AuditEventRecord>
{
    public void Configure(EntityTypeBuilder<AuditEventRecord> builder)
    {
        builder.ToTable("AuditEvents");
        builder.HasKey(x => x.EventId);
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.OccurredAt).IsRequired();
        builder.Property(x => x.IngestedAt).IsRequired();
        builder.Property(x => x.Action).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Outcome).HasMaxLength(16).IsRequired();
        builder.Property(x => x.ActorType).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Subject).HasMaxLength(256);
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasMaxLength(256);
        builder.Property(x => x.ResourceType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ResourceId).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Sensitivity).HasMaxLength(32);
        builder.Property(x => x.CorrelationId).HasMaxLength(128);
        builder.Property(x => x.RequestId).HasMaxLength(128);
        builder.Property(x => x.DataJson);
        builder.Property(x => x.EnvelopeJson).IsRequired();
        builder.Property(x => x.PrevHash).HasMaxLength(64);
        builder.Property(x => x.RecordHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.OccurredAt });
        builder.HasIndex(x => new { x.TenantId, x.CaseId });
    }
}