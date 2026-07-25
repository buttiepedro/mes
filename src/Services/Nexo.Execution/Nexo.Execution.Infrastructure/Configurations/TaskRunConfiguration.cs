using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexo.Execution.Domain;

namespace Nexo.Execution.Infrastructure.Configurations;

/// <summary>
/// <c>execution.task_runs</c> (§2.7.2) — the instantiated task, the <b>unit of imputation</b>. A child of
/// the <see cref="Execution"/> aggregate (the owning <c>executions → task_runs</c> relationship, cascade
/// delete, is declared on <see cref="ExecutionConfiguration"/>); here we map the columns and the task
/// run's own frozen precedences.
/// </summary>
/// <remarks>
/// The policy fields (<c>obligation</c>, <c>required_evidence_kind</c>, <c>min_evidence_count</c>,
/// <c>std_duration_sec</c>) are frozen copies from the definition, so the run enforces its own
/// close/skip/evidence rules without querying Work Model. <c>task_id</c> → <c>work.tasks</c>,
/// <c>assigned_role_id</c> → <c>config.roles</c>, <c>assigned_person_id</c> → <c>master.people</c>,
/// <c>work_center_id</c> / <c>shift_id</c> → <c>config.*</c> and <c>blocked_reason_code_id</c> →
/// <c>config.reason_codes</c> are all mapped as plain <c>uuid</c> columns — logical references without a
/// physical foreign key (§1.9).
/// </remarks>
public sealed class TaskRunConfiguration : IEntityTypeConfiguration<TaskRun>
{
    public void Configure(EntityTypeBuilder<TaskRun> builder)
    {
        builder.ToTable("task_runs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.ExecutionId).HasColumnName("execution_id").IsRequired();

        // LOGICAL reference to work.tasks — uuid WITHOUT a foreign key (§1.9). Never null in this slice (no ad-hoc).
        builder.Property(x => x.TaskId).HasColumnName("task_id");
        builder.Property(x => x.Occurrence).HasColumnName("occurrence").IsRequired();
        builder.Property(x => x.IsAdHoc).HasColumnName("is_ad_hoc").IsRequired();

        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(256);
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(64);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion(ExecutionConfigurationExtensions.TaskRunStatusConverter)
            .HasMaxLength(16)
            .IsRequired();

        // Assignment (role → person is resolved here, not in the template) — logical references (§1.9).
        builder.Property(x => x.AssignedRoleId).HasColumnName("assigned_role_id");
        builder.Property(x => x.AssignedPersonId).HasColumnName("assigned_person_id");

        builder.Property(x => x.AssignmentMode)
            .HasColumnName("assignment_mode")
            .HasConversion(ExecutionConfigurationExtensions.AssignmentModeConverter)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.WorkCenterId).HasColumnName("work_center_id");
        builder.Property(x => x.ShiftId).HasColumnName("shift_id");

        // Frozen standard, adjustable estimate.
        builder.Property(x => x.StandardDurationSeconds)
            .HasColumnName("std_duration_sec")
            .HasColumnType("numeric(18,2)");
        builder.Property(x => x.EstimatedDurationSeconds)
            .HasColumnName("est_duration_sec")
            .HasColumnType("numeric(18,2)");
        builder.Property(x => x.ProgressWeight)
            .HasColumnName("progress_weight")
            .HasColumnType("numeric(5,2)");

        builder.Property(x => x.ActualStartAt).HasColumnName("actual_start_at");
        builder.Property(x => x.ActualEndAt).HasColumnName("actual_end_at");

        // Real time by component (same canonical decomposition as the standard, work-model.md §3.5).
        builder.Property(x => x.ActualSetupSeconds).HasColumnName("actual_setup_sec").IsRequired();
        builder.Property(x => x.ActualExecSeconds).HasColumnName("actual_exec_sec").IsRequired();
        builder.Property(x => x.ActualWaitSeconds).HasColumnName("actual_wait_sec").IsRequired();
        builder.Property(x => x.ActualControlSeconds).HasColumnName("actual_control_sec").IsRequired();
        builder.Property(x => x.ActualClosingSeconds).HasColumnName("actual_closing_sec").IsRequired();

        builder.Property(x => x.ProgressPct).HasColumnName("progress_pct").HasColumnType("numeric(5,2)").IsRequired();

        builder.Property(x => x.ProgressMethod)
            .HasColumnName("progress_method")
            .HasConversion(ExecutionConfigurationExtensions.ProgressMethodConverter)
            .HasMaxLength(16);

        builder.Property(x => x.ProducedQuantity).HasColumnName("produced_qty").HasColumnType("numeric(18,4)");
        builder.Property(x => x.TargetQuantity).HasColumnName("target_qty").HasColumnType("numeric(18,4)");

        builder.Property(x => x.IsOnCriticalPath).HasColumnName("is_on_critical_path").IsRequired();
        builder.Property(x => x.IsMilestone).HasColumnName("is_milestone").IsRequired();
        builder.Property(x => x.MilestoneCommittedDate).HasColumnName("milestone_committed_date");
        builder.Property(x => x.MilestoneReachedAt).HasColumnName("milestone_reached_at");

        builder.Property(x => x.Obligation)
            .HasColumnName("obligation")
            .HasConversion(ExecutionConfigurationExtensions.ObligationConverter)
            .HasMaxLength(16)
            .IsRequired();

        // Frozen evidence requirement used to gate completion (E11).
        builder.Property(x => x.RequiredEvidenceKind)
            .HasColumnName("required_evidence_kind")
            .HasConversion(ExecutionConfigurationExtensions.EvidenceKindConverter)
            .HasMaxLength(32);
        builder.Property(x => x.MinEvidenceCount).HasColumnName("min_evidence_count").IsRequired();

        builder.Property(x => x.BlockedReasonCodeId).HasColumnName("blocked_reason_code_id");
        builder.Property(x => x.BlockedAt).HasColumnName("blocked_at");
        builder.Property(x => x.IsForcedClose).HasColumnName("is_forced_close").IsRequired();
        builder.Property(x => x.SkipReason).HasColumnName("skip_reason").HasMaxLength(512);
        builder.Property(x => x.CloseReason).HasColumnName("close_reason").HasMaxLength(512);
        builder.Property(x => x.Notes).HasColumnName("notes");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.HasQueryFilter(x => x.DeletedAt == null);

        // actual_total_sec is a stored generated column in the DDL; here it is a derived read-only property.
        builder.Ignore(x => x.ActualTotalSeconds);
        builder.Ignore(x => x.IsMandatory);
        builder.Ignore(x => x.HasStarted);
        builder.Ignore(x => x.IsTerminal);
        builder.Ignore(x => x.IsFinished);

        // The run owns its frozen incoming precedences (a value object, not an entity): they travel with it
        // in their own child table. The predecessor is a LOGICAL reference to work.tasks (§1.9).
        builder.OwnsMany(x => x.Precedences, precedence =>
        {
            precedence.ToTable("task_run_precedences");

            precedence.WithOwner().HasForeignKey("TaskRunId");
            precedence.Property<Guid>("TaskRunId").HasColumnName("task_run_id");

            precedence.Property(p => p.PredecessorTaskId).HasColumnName("predecessor_task_id").IsRequired();

            precedence.Property(p => p.Type)
                .HasColumnName("type")
                .HasConversion(ExecutionConfigurationExtensions.DependencyTypeConverter)
                .HasMaxLength(4)
                .IsRequired();

            precedence.Property(p => p.LagSeconds).HasColumnName("lag_sec").IsRequired();

            // A predecessor gates a given run at most once.
            precedence.HasKey("TaskRunId", nameof(TaskRunPrecedence.PredecessorTaskId));
        });

        builder.Metadata.FindNavigation(nameof(TaskRun.Precedences))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        // ux_task_runs_instance: the task instances once per (execution, task, occurrence) among live rows.
        builder.HasIndex(x => new { x.ExecutionId, x.TaskId, x.Occurrence })
            .IsUnique()
            .HasDatabaseName("ux_task_runs_instance")
            .HasFilter($"{ExecutionConfigurationExtensions.LiveRowsFilter} AND task_id IS NOT NULL");

        builder.HasIndex(x => x.ExecutionId).HasDatabaseName("ix_task_runs_exec");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("ix_task_runs_status")
            .HasFilter(ExecutionConfigurationExtensions.LiveRowsFilter);

        builder.HasIndex(x => new { x.AssignedPersonId, x.Status })
            .HasDatabaseName("ix_task_runs_person")
            .HasFilter(ExecutionConfigurationExtensions.LiveRowsFilter);

        builder.HasIndex(x => new { x.WorkCenterId, x.ActualStartAt })
            .HasDatabaseName("ix_task_runs_wc_time")
            .IsDescending(false, true);

        builder.HasIndex(x => new { x.ExecutionId, x.MilestoneCommittedDate })
            .HasDatabaseName("ix_task_runs_milestone")
            .HasFilter("is_milestone");
    }
}
