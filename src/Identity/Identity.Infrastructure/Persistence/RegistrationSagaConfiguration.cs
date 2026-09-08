using Identity.Domain.Registration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence;

public sealed class RegistrationSagaConfiguration : IEntityTypeConfiguration<RegistrationSaga>
{
    public void Configure(EntityTypeBuilder<RegistrationSaga> builder)
    {
        builder.ToTable("RegistrationSagas");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.IdempotencyKey)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(s => s.IdempotencyKey)
            .IsUnique();

        builder.Property(s => s.Email)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(s => s.Status)
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(s => s.Status);

        builder.Property(s => s.Auth0UserId)
            .HasMaxLength(128);

        builder.Property(s => s.LastError)
            .HasMaxLength(500);

        builder.Property(s => s.AttemptCount)
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .IsRequired();
    }
}
