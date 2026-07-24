using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexo.MasterData.Domain;

namespace Nexo.MasterData.Infrastructure.Configurations;

/// <summary>
/// <c>master.people</c> — the operational dimension of a person (docs/design/03-data-schema.md §2.5.3).
/// No hourly rate: cost is deferred to V1 (§2.5.5).
/// </summary>
public sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("people");

        builder.ConfigureMasterRecord(codeMaxLength: 64);

        builder.Property(x => x.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(256)
            .IsRequired();

        // LOGICAL references, nullable and WITHOUT foreign keys (§1.9):
        //   - default_role_id -> config.roles, site_id -> config.sites, line_id -> config.lines:
        //     the `config` schema does not exist yet (it belongs to a service that is not built),
        //     so declaring a FK here would fail at migration time.
        //   - user_id -> the global identity of the Control Plane, a physically separate database.
        //     A person may exist WITHOUT a user: an operator who clocks in with a badge has no account.
        // Integrity is validated in the application layer.
        builder.Property(x => x.DefaultRoleId).HasColumnName("default_role_id");
        builder.Property(x => x.SiteId).HasColumnName("site_id");
        builder.Property(x => x.LineId).HasColumnName("line_id");
        builder.Property(x => x.UserId).HasColumnName("user_id");

        builder.Property(x => x.Calendar)
            .HasColumnName("calendar")
            .HasColumnType("jsonb");

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName("ux_people_code")
            .HasFilter(MasterRecordConfigurationExtensions.LiveRowsFilter);

        builder.HasIndex(x => x.UserId)
            .IsUnique()
            .HasDatabaseName("ux_people_user")
            .HasFilter($"{MasterRecordConfigurationExtensions.LiveRowsFilter} AND user_id IS NOT NULL");

        builder.HasIndex(x => x.SiteId).HasDatabaseName("ix_people_site_id");

        builder.HasUniqueExternalRef("ux_people_external_ref");
    }
}
