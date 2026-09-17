using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Document.Domain.Documents;

namespace Document.Infrastructure.Persistence;

public sealed class DocumentAccessLogConfiguration : IEntityTypeConfiguration<DocumentAccessLog>
{
    public void Configure(EntityTypeBuilder<DocumentAccessLog> builder)
    {
        builder.ToTable("DocumentAccessLogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.DocumentId).IsRequired();
        builder.Property(x => x.Action).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.Detail).HasMaxLength(500);
        builder.Property(x => x.OccurredAt).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.DocumentId });
    }
}
