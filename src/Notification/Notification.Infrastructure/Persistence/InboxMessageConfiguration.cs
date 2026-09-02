using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notification.Domain.Inbox;

namespace Notification.Infrastructure.Persistence;

public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("InboxMessages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Type).HasMaxLength(128).IsRequired();
        builder.Property(m => m.Payload).HasMaxLength(4000).IsRequired();
        builder.Property(m => m.IdempotencyKey).HasMaxLength(256).IsRequired();
        builder.Property(m => m.Status).HasMaxLength(32).IsRequired();
        builder.Property(m => m.LastError).HasMaxLength(2000);
        builder.HasIndex(m => m.IdempotencyKey).IsUnique();
        builder.HasIndex(m => new { m.Status, m.NextAttemptAt });
    }
}