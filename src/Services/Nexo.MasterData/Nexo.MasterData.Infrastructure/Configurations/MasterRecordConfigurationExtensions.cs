using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Nexo.MasterData.Domain;

namespace Nexo.MasterData.Infrastructure.Configurations;

/// <summary>
/// Shared mapping for everything <see cref="MasterRecord"/> contributes to a master-data table:
/// natural key, governance, lifecycle status, external reference, the standard audit block and the
/// soft-delete columns (docs/design/03-data-schema.md §1.3 and §1.4).
/// </summary>
internal static class MasterRecordConfigurationExtensions
{
    /// <summary>Filter used by every partial unique index over a natural key: only live rows compete.</summary>
    public const string LiveRowsFilter = "deleted_at IS NULL";

    /// <summary>Lifecycle status persisted as lower-case text (<c>active</c> | <c>archived</c>).</summary>
    public static readonly ValueConverter<MasterStatus, string> StatusConverter = new(
        status => status.ToString().ToLowerInvariant(),
        value => Enum.Parse<MasterStatus>(value, true));

    /// <summary>Governance persisted as lower-case text (<c>local</c> | <c>mirror</c> | <c>linked</c> | <c>divergent</c>).</summary>
    public static readonly ValueConverter<MasterGovernance, string> GovernanceConverter = new(
        governance => governance.ToString().ToLowerInvariant(),
        value => Enum.Parse<MasterGovernance>(value, true));

    public static EntityTypeBuilder<TRecord> ConfigureMasterRecord<TRecord>(
        this EntityTypeBuilder<TRecord> builder,
        int codeMaxLength = 64)
        where TRecord : MasterRecord
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.Code)
            .HasColumnName("code")
            .HasMaxLength(codeMaxLength)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion(StatusConverter)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.Governance)
            .HasColumnName("governance")
            .HasConversion(GovernanceConverter)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.ExternalRef)
            .HasColumnName("external_ref")
            .HasMaxLength(128);

        // Standard audit block (§1.3). created_by / updated_by / deleted_by are LOGICAL references to
        // the global identity (Control Plane): uuid without a foreign key, since the databases are
        // physically separate (§1.9).
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        // Soft-delete (§1.4): deleted_at IS NULL means the row is live.
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.HasQueryFilter(x => x.DeletedAt == null);

        // Computed members that exist only for the domain / integration events.
        builder.Ignore(x => x.Catalog);
        builder.Ignore(x => x.DisplayName);
        builder.Ignore(x => x.IsArchived);
        builder.Ignore(x => x.DomainEvents);

        return builder;
    }

    /// <summary>
    /// Partial unique index over the ERP reference: only live rows that actually carry one compete.
    /// </summary>
    public static EntityTypeBuilder<TRecord> HasUniqueExternalRef<TRecord>(
        this EntityTypeBuilder<TRecord> builder,
        string indexName)
        where TRecord : MasterRecord
    {
        builder.HasIndex(x => x.ExternalRef)
            .IsUnique()
            .HasDatabaseName(indexName)
            .HasFilter($"{LiveRowsFilter} AND external_ref IS NOT NULL");

        return builder;
    }
}
