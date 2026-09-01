using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationEntity = Notification.Domain.Notifications.Notification;

namespace Notification.Infrastructure.Persistence;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<NotificationEntity>
{
    public void Configure(EntityTypeBuilder<NotificationEntity> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Channel).HasMaxLength(32).IsRequired();
        builder.Property(n => n.Recipient).HasMaxLength(320).IsRequired();
        builder.Property(n => n.TemplateKey).HasMaxLength(128).IsRequired();
        builder.Property(n => n.TemplateData).HasMaxLength(4000).IsRequired();
        builder.Property(n => n.RenderedSubject).HasMaxLength(500).IsRequired();
        builder.Property(n => n.RenderedBody).HasMaxLength(8000).IsRequired();
        builder.Property(n => n.Source).HasMaxLength(64).IsRequired();
        builder.Property(n => n.CorrelationId).HasMaxLength(128);
        builder.Property(n => n.Status).HasMaxLength(32).IsRequired();
        builder.Property(n => n.IdempotencyKey).HasMaxLength(256).IsRequired();
        builder.Property(n => n.PayloadFingerprint).HasMaxLength(1024).IsRequired();
        builder.HasIndex(n => n.IdempotencyKey).IsUnique();
        builder.HasMany(n => n.Attempts)
            .WithOne()
            .HasForeignKey(a => a.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}