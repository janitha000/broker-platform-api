using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Origination.Domain.Cases;

namespace Origination.Infrastructure.Persistence;

public sealed class CaseConfiguration : IEntityTypeConfiguration<Case>
{
    public void Configure(EntityTypeBuilder<Case> builder)
    {
        builder.ToTable("Cases");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.BrokerId)
            .IsRequired();

        builder.Property(c => c.TenantId)
            .IsRequired();

        builder.Property(c => c.InquiryNotes)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.OwnsOne(c => c.FactFind, ff =>
        {
            ff.Property(x => x.Objectives).HasMaxLength(4000);
            ff.Property(x => x.Income).HasPrecision(18, 2);
            ff.Property(x => x.Expenses).HasPrecision(18, 2);
            ff.Property(x => x.Assets).HasPrecision(18, 2);
            ff.Property(x => x.Debts).HasPrecision(18, 2);
            ff.Property(x => x.CompletedAt);
        });

        builder.Property(c => c.CreatedAt)
            .IsRequired();
    }
}