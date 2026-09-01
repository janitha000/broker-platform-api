using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notification.Domain.Notifications;

namespace Notification.Infrastructure.Persistence;

public sealed class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("Templates");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Key).HasMaxLength(128).IsRequired();
        builder.Property(t => t.Channel).HasMaxLength(32).IsRequired();
        builder.Property(t => t.Locale).HasMaxLength(16).IsRequired();
        builder.Property(t => t.SubjectTemplate).HasMaxLength(500).IsRequired();
        builder.Property(t => t.BodyTemplate).HasMaxLength(8000).IsRequired();
        builder.HasIndex(t => new { t.Key, t.Channel, t.Locale, t.Version }).IsUnique();

        builder.HasData(new NotificationTemplate
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeee1"),
            Key = "case.fact-find-completed",
            Channel = "Email",
            Locale = "en-AU",
            Version = 1,
            SubjectTemplate = "Fact-find completed for case {{caseId}}",
            BodyTemplate = "Hi {{brokerName}}, fact-find is complete for case {{caseId}}.",
            IsActive = true,
        });
    }
}