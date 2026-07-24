using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexo.WorkModel.Domain;

namespace Nexo.WorkModel.Infrastructure.Configurations;

/// <summary>
/// <c>work.processes</c> — the process library. Identity (<c>code</c>) is stable across versions (W13).
/// </summary>
/// <remarks>
/// <b>No foreign key leaves this bounded context.</b> <c>output_item_id</c> and <c>output_uom_id</c>
/// point at <c>master.*</c> (Nexo.MasterData) and <c>site_id</c> / <c>area_id</c> / <c>line_id</c> at
/// <c>config.*</c>: the design declares them as FKs, but the services of a tenant share one physical
/// database while owning <b>separate migration histories</b>. A physical FK would force one service's
/// migration to run before another's, so they are mapped as plain <c>uuid</c> columns — logical
/// references (§1.9). The same goes for <c>current_version_id</c>: the design closes that cycle with a
/// deferrable FK, which EF Core cannot express, and the invariant already lives in the aggregate.
/// </remarks>
public sealed class ProcessConfiguration : IEntityTypeConfiguration<Process>
{
    public void Configure(EntityTypeBuilder<Process> builder)
    {
        builder.ToTable("processes");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.Code)
            .HasColumnName("code")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Profile)
            .HasColumnName("profile")
            .HasConversion(WorkModelConfigurationExtensions.ProfileConverter)
            .HasMaxLength(16)
            .IsRequired();

        // Published version in force. No foreign key: the design uses a deferrable one to close the
        // processes <-> process_versions cycle, and CB15 is enforced by the aggregate plus the partial
        // unique index ux_process_versions_published.
        builder.Property(x => x.CurrentVersionId).HasColumnName("current_version_id");

        // LOGICAL references to master.* / config.* — uuid WITHOUT a foreign key (§1.9).
        builder.Property(x => x.OutputItemId).HasColumnName("output_item_id");
        builder.Property(x => x.OutputUomId).HasColumnName("output_uom_id");
        builder.Property(x => x.SiteId).HasColumnName("site_id");
        builder.Property(x => x.AreaId).HasColumnName("area_id");
        builder.Property(x => x.LineId).HasColumnName("line_id");

        builder.Property(x => x.EvidencePolicy)
            .HasColumnName("evidence_policy")
            .HasConversion(WorkModelConfigurationExtensions.EvidencePolicyConverter)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.SkipPolicy)
            .HasColumnName("skip_policy")
            .HasConversion(WorkModelConfigurationExtensions.SkipPolicyConverter)
            .HasMaxLength(16)
            .IsRequired();

        // tags text[]: exposed read-only over the _tags backing field.
        builder.Property(x => x.Tags)
            .HasColumnName("tags")
            .HasColumnType("text[]")
            .HasConversion(
                tags => tags.ToArray(),
                value => value.ToArray(),
                new ValueComparer<IReadOnlyCollection<string>>(
                    (left, right) => left!.SequenceEqual(right!),
                    tags => tags.Aggregate(0, (hash, tag) => HashCode.Combine(hash, tag.GetHashCode())),
                    tags => tags.ToArray()))
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .IsRequired();

        builder.Property(x => x.ExternalRef)
            .HasColumnName("external_ref")
            .HasMaxLength(128);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion(WorkModelConfigurationExtensions.ProcessStatusConverter)
            .HasMaxLength(16)
            .IsRequired();

        // Standard audit block (§1.3): created_by / updated_by / deleted_by are LOGICAL references to
        // the global identity (Control Plane), uuid without a foreign key (§1.9).
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        // Soft-delete (§1.4): deleted_at IS NULL means the row is live.
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.Ignore(x => x.IsArchived);
        builder.Ignore(x => x.HasPublishedVersion);
        builder.Ignore(x => x.DomainEvents);

        // W13: the code is unique among live rows.
        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName("ux_processes_code")
            .HasFilter(WorkModelConfigurationExtensions.LiveRowsFilter);

        builder.HasIndex(x => x.Profile)
            .HasDatabaseName("ix_processes_profile")
            .HasFilter(WorkModelConfigurationExtensions.LiveRowsFilter);
    }
}
