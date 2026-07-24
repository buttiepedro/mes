using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexo.BuildingBlocks.Messaging;

namespace Nexo.MasterData.Infrastructure.Configurations;

/// <summary>
/// Transactional outbox table, owned by this service and living in its own domain schema.
/// </summary>
/// <remarks>
/// Services of a tenant share one physical database, so a single <c>platform.outbox_messages</c> table
/// would be created by whichever service migrated first and then collide for every other one (Postgres
/// 42P07). The outbox belongs to the service's transactional boundary, so each service owns its own
/// table in its own schema: no migration-ownership ambiguity and no ordering dependency between
/// services. Each service's relay drains its own table.
/// </remarks>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages", MasterDataDbContext.DomainSchema);

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
