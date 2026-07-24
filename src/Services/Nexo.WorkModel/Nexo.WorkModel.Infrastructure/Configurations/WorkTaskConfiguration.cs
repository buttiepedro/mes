using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexo.WorkModel.Domain;

namespace Nexo.WorkModel.Infrastructure.Configurations;

/// <summary>
/// <c>work.tasks</c> — a task definition inside a <see cref="ProcessVersion"/>. A child of the version
/// aggregate: it is created, edited and removed through the root, so W10 (published versions are
/// immutable) holds in one place. The <c>process_versions -&gt; tasks</c> relationship (owning side,
/// cascade delete) is declared on <see cref="ProcessVersionConfiguration"/>; here we map the columns and
/// the task's own <c>tasks -&gt; task_inputs</c> ownership.
/// </summary>
public sealed class WorkTaskConfiguration : IEntityTypeConfiguration<WorkTask>
{
    public void Configure(EntityTypeBuilder<WorkTask> builder)
    {
        builder.ToTable("tasks");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.ProcessVersionId).HasColumnName("process_version_id").IsRequired();

        builder.Property(x => x.Code)
            .HasColumnName("code")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Instructions).HasColumnName("instructions");

        builder.Property(x => x.DisplaySeq).HasColumnName("display_seq").IsRequired();

        builder.Property(x => x.EstimatedDurationSeconds)
            .HasColumnName("estimated_duration_sec")
            .HasColumnType("numeric(18,2)");

        builder.Property(x => x.StandardDurationSeconds)
            .HasColumnName("standard_duration_sec")
            .HasColumnType("numeric(18,2)");

        builder.Property(x => x.ProgressWeight)
            .HasColumnName("progress_weight")
            .HasColumnType("numeric(5,2)");

        // LOGICAL references — uuid WITHOUT a foreign key (§1.9): responsible_role_id -> config.roles,
        // suggested_person_id -> master.people. Neither schema is owned by this bounded context.
        builder.Property(x => x.ResponsibleRoleId).HasColumnName("responsible_role_id").IsRequired();
        builder.Property(x => x.SuggestedPersonId).HasColumnName("suggested_person_id");

        builder.Property(x => x.Completion)
            .HasColumnName("completion")
            .HasConversion(WorkModelConfigurationExtensions.CompletionKindConverter)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.CompletionSpec)
            .HasColumnName("completion_spec")
            .HasColumnType("jsonb");

        builder.Property(x => x.Obligation)
            .HasColumnName("obligation")
            .HasConversion(WorkModelConfigurationExtensions.ObligationConverter)
            .HasMaxLength(16)
            .IsRequired();

        // Nullable enum: null inherits the process policy (task > process > tenant).
        builder.Property(x => x.EvidencePolicyOverride)
            .HasColumnName("evidence_policy")
            .HasConversion(WorkModelConfigurationExtensions.EvidencePolicyConverter)
            .HasMaxLength(16);

        builder.Property(x => x.RequiredEvidenceKind)
            .HasColumnName("required_evidence_kind")
            .HasConversion(WorkModelConfigurationExtensions.EvidenceKindConverter)
            .HasMaxLength(32);

        builder.Property(x => x.MinEvidenceCount).HasColumnName("min_evidence_count").IsRequired();

        builder.Property(x => x.RequiredCapability)
            .HasColumnName("required_capability")
            .HasMaxLength(64);

        builder.Property(x => x.RequiredAssetType)
            .HasColumnName("required_asset_type")
            .HasMaxLength(64);

        builder.Property(x => x.IsMilestone).HasColumnName("is_milestone").IsRequired();
        builder.Property(x => x.IsParallelizable).HasColumnName("is_parallelizable").IsRequired();
        builder.Property(x => x.IsRepeatable).HasColumnName("is_repeatable").IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.Ignore(x => x.IsMandatory);

        // The task owns its standard consumption: inputs travel with it and die with it.
        builder.HasMany(x => x.Inputs)
            .WithOne()
            .HasForeignKey(input => input.TaskId)
            .HasConstraintName("fk_task_inputs_task")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(WorkTask.Inputs))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        // Task code is the natural key of the task within its version.
        builder.HasIndex(x => new { x.ProcessVersionId, x.Code })
            .IsUnique()
            .HasDatabaseName("ux_tasks_version_code")
            .HasFilter(WorkModelConfigurationExtensions.LiveRowsFilter);

        builder.HasIndex(x => x.ResponsibleRoleId)
            .HasDatabaseName("ix_tasks_responsible_role_id")
            .HasFilter(WorkModelConfigurationExtensions.LiveRowsFilter);
    }
}
