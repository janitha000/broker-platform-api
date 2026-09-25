using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Origination.Infrastructure.Messaging.Sagas;

namespace Origination.Infrastructure.Persistence;

public sealed class CaseLifecycleStateConfiguration : IEntityTypeConfiguration<CaseLifecycleState>
{
    public void Configure(EntityTypeBuilder<CaseLifecycleState> builder)
    {
        builder.ToTable("CaseLifecycleState");
        builder.HasKey(x => x.CorrelationId);
        builder.Property(x => x.CurrentState).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.CurrentState);
    }
}
