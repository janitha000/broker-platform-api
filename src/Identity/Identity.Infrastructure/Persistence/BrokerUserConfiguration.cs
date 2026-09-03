using Identity.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence;

public sealed class BrokerUserConfiguration : IEntityTypeConfiguration<BrokerUser>
{
    public void Configure(EntityTypeBuilder<BrokerUser> builder)
    {
        builder.ToTable("BrokerUsers");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.TenantId)
            .IsRequired();

        builder.Property(u => u.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasMaxLength(320)
            .IsRequired();

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.Auth0UserId)
            .HasMaxLength(128);

        builder.HasIndex(u => u.Auth0UserId)
            .IsUnique()
            .HasFilter("[Auth0UserId] IS NOT NULL");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
