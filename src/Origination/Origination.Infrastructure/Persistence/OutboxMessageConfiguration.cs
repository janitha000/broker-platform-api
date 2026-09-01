using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Origination.Domain.Outbox;

namespace Origination.Infrastructure.Persistence;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Type).HasMaxLength(128).IsRequired();
        builder.Property(m => m.Payload).HasMaxLength(4000).IsRequired();
        builder.Property(m => m.IdempotencyKey).HasMaxLength(256).IsRequired();
        builder.HasIndex(m => m.IdempotencyKey).IsUnique();
        builder.HasIndex(m => m.PublishedAt);
    }
}