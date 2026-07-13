using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexo.BuildingBlocks.Messaging;

namespace Nexo.Production.Infrastructure.Configurations;

/// <summary>
/// Transactional outbox table. Lives in the shared <c>platform</c> schema (not the domain schema),
/// so the same physical layout is used across services within the tenant database.
/// </summary>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages", ProductionDbContext.PlatformSchema);

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.Type).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Payload).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.OccurredOn).IsRequired();
        builder.Property(x => x.ProcessedOn);
        builder.Property(x => x.Error);

        // Partial-index equivalent for the relay sweep (pending = not yet processed).
        builder.HasIndex(x => x.ProcessedOn);
        builder.HasIndex(x => x.TenantId);
    }
}
