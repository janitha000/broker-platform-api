using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notification.Domain.Notifications;

namespace Notification.Infrastructure.Persistence;

public sealed class DeliveryAttemptConfiguration : IEntityTypeConfiguration<DeliveryAttempt>
{
    public void Configure(EntityTypeBuilder<DeliveryAttempt> builder)
    {
        builder.ToTable("DeliveryAttempts");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.ProviderMessageId).HasMaxLength(256);
        builder.Property(a => a.ErrorCode).HasMaxLength(128);
        builder.Property(a => a.ErrorMessage).HasMaxLength(1000);
    }
}