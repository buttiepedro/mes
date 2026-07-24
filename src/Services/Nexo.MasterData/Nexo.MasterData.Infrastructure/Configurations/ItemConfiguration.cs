using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Nexo.MasterData.Domain;

namespace Nexo.MasterData.Infrastructure.Configurations;

/// <summary>
/// <c>master.items</c> — one catalog for products and inputs, because they are <b>roles</b> of the
/// same item (docs/design/03-data-schema.md §2.5.2). <c>roles</c> is a Postgres <c>text[]</c>, the same
/// <c>text[] + CHECK + GIN</c> pattern already adopted for <c>config.reason_codes.domains</c> (MOD-03).
/// </summary>
public sealed class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    /// <summary>Tracking persisted as lower-case text (<c>none</c> | <c>batch</c> | <c>serial</c>).</summary>
    private static readonly ValueConverter<TrackingMode, string> TrackingConverter = new(
        tracking => tracking.ToString().ToLowerInvariant(),
        value => Enum.Parse<TrackingMode>(value, true));

    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("items");

        builder.ConfigureMasterRecord(codeMaxLength: 64);

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(256)
            .IsRequired();

        // FK to master.uom — the only physical foreign key of this aggregate: both tables live in
        // this service's schema.
        builder.Property(x => x.BaseUomId)
            .HasColumnName("base_uom_id")
            .IsRequired();

        builder.HasOne<Uom>()
            .WithMany()
            .HasForeignKey(x => x.BaseUomId)
            .HasConstraintName("fk_items_uom")
            .OnDelete(DeleteBehavior.Restrict);

        // roles text[]: the collection is exposed read-only over the _roles backing field.
        builder.Property(x => x.Roles)
            .HasColumnName("roles")
            .HasColumnType("text[]")
            .HasConversion(
                roles => ItemRoleDbValues.ToDbValues(roles),
                value => ItemRoleDbValues.FromDbValues(value),
                new ValueComparer<IReadOnlyCollection<ItemRole>>(
                    (left, right) => left!.SequenceEqual(right!),
                    roles => roles.Aggregate(0, (hash, role) => HashCode.Combine(hash, role.GetHashCode())),
                    roles => ItemRoleDbValues.Copy(roles)))
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .IsRequired();

        builder.Property(x => x.Category)
            .HasColumnName("category")
            .HasMaxLength(64);

        builder.Property(x => x.Family)
            .HasColumnName("family")
            .HasMaxLength(128);

        builder.Property(x => x.Tracking)
            .HasColumnName("tracking")
            .HasConversion(TrackingConverter)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.IdealCycleTime)
            .HasColumnName("ideal_cycle_time")
            .HasColumnType("numeric(18,6)");

        // LOGICAL reference to work.processes (Nexo.WorkModel): nullable and WITHOUT a foreign key,
        // because that schema does not exist in this service's model (§1.9).
        builder.Property(x => x.DefaultProcessId)
            .HasColumnName("default_process_id");

        builder.Property(x => x.QualitySpecs)
            .HasColumnName("quality_specs")
            .HasColumnType("jsonb");

        builder.Property(x => x.LastSyncedAt)
            .HasColumnName("last_synced_at");

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName("ux_items_code")
            .HasFilter(MasterRecordConfigurationExtensions.LiveRowsFilter);

        builder.HasIndex(x => x.Roles)
            .HasDatabaseName("ix_items_roles")
            .HasMethod("gin");

        builder.HasIndex(x => x.BaseUomId).HasDatabaseName("ix_items_base_uom_id");

        builder.HasUniqueExternalRef("ux_items_external_ref");
    }
}

/// <summary>
/// Storage values of <see cref="ItemRole"/> inside the <c>roles text[]</c> column
/// (<c>product</c> / <c>input</c>, exactly as the CHECK constraint of the design spells them).
/// </summary>
internal static class ItemRoleDbValues
{
    public static string ToDbValue(ItemRole role) => role.ToString().ToLowerInvariant();

    public static ItemRole FromDbValue(string value) => Enum.Parse<ItemRole>(value, true);

    public static string[] ToDbValues(IReadOnlyCollection<ItemRole> roles)
        => roles.Select(ToDbValue).ToArray();

    public static IReadOnlyCollection<ItemRole> FromDbValues(string[] values)
        => values.Select(FromDbValue).ToArray();

    public static IReadOnlyCollection<ItemRole> Copy(IReadOnlyCollection<ItemRole> roles) => roles.ToArray();
}
