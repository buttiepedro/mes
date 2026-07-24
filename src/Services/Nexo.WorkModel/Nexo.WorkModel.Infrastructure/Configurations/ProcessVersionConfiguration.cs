using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexo.WorkModel.Domain;

namespace Nexo.WorkModel.Infrastructure.Configurations;

/// <summary>
/// <c>work.process_versions</c> — the root of the graph. Owns its tasks and its precedences, so the
/// whole aggregate is loaded and saved as one unit.
/// </summary>
/// <remarks>
/// <b>CB15 — one single published version per process</b> is guaranteed in the database by the partial
/// unique index <c>ux_process_versions_published</c> (<c>where state = 'published' and deleted_at is
/// null</c>), not only by the application: the aggregate can be raced, the index cannot.
/// </remarks>
public sealed class ProcessVersionConfiguration : IEntityTypeConfiguration<ProcessVersion>
{
    public void Configure(EntityTypeBuilder<ProcessVersion> builder)
    {
        builder.ToTable("process_versions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.ProcessId).HasColumnName("process_id").IsRequired();

        builder.Property(x => x.VersionNo)
            .HasColumnName("version_no")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.VersionMajor).HasColumnName("version_major").IsRequired();
        builder.Property(x => x.VersionMinor).HasColumnName("version_minor").IsRequired();
        builder.Property(x => x.VersionPatch).HasColumnName("version_patch").IsRequired();

        builder.Property(x => x.State)
            .HasColumnName("state")
            .HasConversion(WorkModelConfigurationExtensions.VersionStateConverter)
            .HasMaxLength(16)
            .IsRequired();

        // FROZEN copy of the process profile: changing it demands a major version (W11).
        builder.Property(x => x.Profile)
            .HasColumnName("profile")
            .HasConversion(WorkModelConfigurationExtensions.ProfileConverter)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.ChangeReason).HasColumnName("change_reason").HasMaxLength(512);
        builder.Property(x => x.PublishedAt).HasColumnName("published_at");
        builder.Property(x => x.SuspendedAt).HasColumnName("suspended_at");

        builder.Property(x => x.WorkloadSeconds)
            .HasColumnName("workload_sec")
            .HasColumnType("numeric(18,2)");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.Ignore(x => x.IsEditable);
        builder.Ignore(x => x.IsPublished);
        builder.Ignore(x => x.DomainEvents);

        // The version owns the graph: tasks and precedences travel with it and die with it.
        builder.HasMany(x => x.Tasks)
            .WithOne()
            .HasForeignKey(task => task.ProcessVersionId)
            .HasConstraintName("fk_tasks_version")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Dependencies)
            .WithOne()
            .HasForeignKey(dependency => dependency.ProcessVersionId)
            .HasConstraintName("fk_task_dep_version")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(ProcessVersion.Tasks))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(ProcessVersion.Dependencies))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        // FK to work.processes — a real foreign key: both tables live in this service's schema.
        builder.HasOne<Process>()
            .WithMany()
            .HasForeignKey(x => x.ProcessId)
            .HasConstraintName("fk_process_versions_process")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ProcessId, x.VersionNo })
            .IsUnique()
            .HasDatabaseName("ux_process_versions_no")
            .HasFilter(WorkModelConfigurationExtensions.LiveRowsFilter);

        // CB15: ONE published version per process, guaranteed in the database and not only in the app.
        builder.HasIndex(x => x.ProcessId)
            .IsUnique()
            .HasDatabaseName("ux_process_versions_published")
            .HasFilter($"state = 'published' AND {WorkModelConfigurationExtensions.LiveRowsFilter}");

        builder.HasIndex(x => x.State)
            .HasDatabaseName("ix_process_versions_state")
            .HasFilter(WorkModelConfigurationExtensions.LiveRowsFilter);
    }
}
