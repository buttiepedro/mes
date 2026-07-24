using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexo.MasterData.Domain;

namespace Nexo.MasterData.Infrastructure.Configurations;

/// <summary>
/// <c>master.customers</c> — deliberately poor (docs/design/03-data-schema.md §2.5.4): no commercial
/// terms, no prices, no invoicing. There is no <c>orders</c> table either: the commitment is an
/// attribute of the Execution, not a catalog.
/// </summary>
public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");

        builder.ConfigureMasterRecord(codeMaxLength: 64);

        builder.Property(x => x.LegalName)
            .HasColumnName("legal_name")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.TaxId)
            .HasColumnName("tax_id")
            .HasMaxLength(32);

        builder.Property(x => x.Contact)
            .HasColumnName("contact")
            .HasColumnType("jsonb");

        builder.Property(x => x.Notes)
            .HasColumnName("notes");

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName("ux_customers_code")
            .HasFilter(MasterRecordConfigurationExtensions.LiveRowsFilter);

        builder.HasUniqueExternalRef("ux_customers_external_ref");
    }
}
