using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Nexo.MasterData.Domain;

namespace Nexo.MasterData.Infrastructure.Configurations;

/// <summary>
/// <c>master.uom</c> — canonical home of the unit-of-measure catalog.
/// </summary>
/// <remarks>
/// The design (docs/design/03-data-schema.md §2.5.1) relocates the table with
/// <c>alter table config.uom set schema master</c>, but the <c>config</c> schema does not exist in the
/// real database: the <c>InitialCreate</c> migration only created <c>production.*</c> and
/// <c>platform.outbox_messages</c>. The table is therefore <b>created new</b> here, and the
/// backwards-compatibility view <c>config.uom</c> is not created either — nothing reads it yet.
/// </remarks>
public sealed class UomConfiguration : IEntityTypeConfiguration<Uom>
{
    /// <summary>Magnitude persisted as lower-case text (<c>mass</c>, <c>length</c>, <c>volume</c>, ...).</summary>
    private static readonly ValueConverter<UomMagnitude, string> MagnitudeConverter = new(
        magnitude => magnitude.ToString().ToLowerInvariant(),
        value => Enum.Parse<UomMagnitude>(value, true));

    public void Configure(EntityTypeBuilder<Uom> builder)
    {
        builder.ToTable("uom");

        builder.ConfigureMasterRecord(codeMaxLength: 32);

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.Symbol)
            .HasColumnName("symbol")
            .HasMaxLength(16)
            .IsRequired();

        // Nullable enum: the non-nullable converter is applied to the nullable property, so a row
        // without magnitude stores SQL NULL instead of an empty string.
        builder.Property(x => x.Magnitude)
            .HasColumnName("magnitude")
            .HasConversion(MagnitudeConverter)
            .HasMaxLength(16);

        builder.Property(x => x.FactorToBase)
            .HasColumnName("factor_to_base")
            .HasColumnType("numeric(18,8)")
            .IsRequired();

        builder.Property(x => x.IsBase)
            .HasColumnName("is_base")
            .IsRequired();

        builder.Property(x => x.Decimals)
            .HasColumnName("decimals")
            .IsRequired();

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName("ux_uom_code")
            .HasFilter(MasterRecordConfigurationExtensions.LiveRowsFilter);

        // A single base unit per magnitude, among live rows only.
        builder.HasIndex(x => x.Magnitude)
            .IsUnique()
            .HasDatabaseName("ux_uom_base_per_magnitude")
            .HasFilter($"is_base AND {MasterRecordConfigurationExtensions.LiveRowsFilter}");

        builder.HasUniqueExternalRef("ux_uom_external_ref");
    }
}
