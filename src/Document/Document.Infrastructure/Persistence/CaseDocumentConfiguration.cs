using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Document.Domain.Documents;

namespace Document.Infrastructure.Persistence;

public sealed class CaseDocumentConfiguration : IEntityTypeConfiguration<CaseDocument>
{
    public void Configure(EntityTypeBuilder<CaseDocument> builder)
    {
        builder.ToTable("CaseDocuments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.CaseId).IsRequired();
        builder.Property(x => x.UploadedByBrokerId).IsRequired();
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Sensitivity).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Sha256).HasMaxLength(64).IsRequired();
        builder.Property(x => x.LandingKey).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.CleanKey).HasMaxLength(1024);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired();
        builder.Property(x => x.RejectionReason).HasMaxLength(500);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.TenantId, x.CaseId });
        builder.HasIndex(x => new { x.TenantId, x.IdempotencyKey }).IsUnique();
    }
}
