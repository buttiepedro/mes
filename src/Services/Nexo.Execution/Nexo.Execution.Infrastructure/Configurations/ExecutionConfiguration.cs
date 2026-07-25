using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexo.Execution.Domain;

namespace Nexo.Execution.Infrastructure.Configurations;

/// <summary>
/// <c>execution.executions</c> (§2.7.1) — the run and the <b>aggregate root</b>. It owns its task runs,
/// its input consumptions and its evidence, so the whole graph is loaded and saved as one unit.
/// </summary>
/// <remarks>
/// <b>No foreign key leaves this bounded context.</b> The frozen template (<c>process_id</c> /
/// <c>process_version_id</c> → <c>work.*</c>), the batch objective (<c>target_item_id</c> /
/// <c>target_uom_id</c> → <c>master.*</c>), the project commitment (<c>customer_id</c> /
/// <c>deliverable_item_id</c> → <c>master.*</c>) and the physical scope (<c>site_id</c> / <c>area_id</c> /
/// <c>line_id</c> / <c>work_center_id</c> → <c>config.*</c>) are all mapped as plain <c>uuid</c> columns —
/// logical references without a physical foreign key (§1.9) — because the services of a tenant share one
/// physical database while owning <b>separate migration histories</b>. <b>No cost (MOD-17).</b>
/// </remarks>
public sealed class ExecutionConfiguration : IEntityTypeConfiguration<Domain.Execution>
{
    public void Configure(EntityTypeBuilder<Domain.Execution> builder)
    {
        builder.ToTable("executions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.Code)
            .HasColumnName("code")
            .HasMaxLength(64)
            .IsRequired();

        // FROZEN template (E1/E2): LOGICAL references to work.processes / work.process_versions (§1.9).
        builder.Property(x => x.ProcessId).HasColumnName("process_id").IsRequired();
        builder.Property(x => x.ProcessVersionId).HasColumnName("process_version_id").IsRequired();
        builder.Property(x => x.VersionNo).HasColumnName("version_no").HasMaxLength(32);

        builder.Property(x => x.Flavor)
            .HasColumnName("flavor")
            .HasConversion(ExecutionConfigurationExtensions.FlavorConverter)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion(ExecutionConfigurationExtensions.ExecutionStatusConverter)
            .HasMaxLength(16)
            .IsRequired();

        // --- Trigger (polymorphic, may be external — no foreign key) -------------------------------
        builder.Property(x => x.TriggerKind)
            .HasColumnName("trigger_kind")
            .HasConversion(ExecutionConfigurationExtensions.TriggerKindConverter)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.TriggerRefKind).HasColumnName("trigger_ref_kind").HasMaxLength(64);
        builder.Property(x => x.TriggerRefId).HasColumnName("trigger_ref_id");
        builder.Property(x => x.TriggerExternalRef).HasColumnName("trigger_external_ref").HasMaxLength(128);

        // --- Batch objective — LOGICAL references to master.* (§1.9) -------------------------------
        builder.Property(x => x.TargetItemId).HasColumnName("target_item_id");
        builder.Property(x => x.TargetQuantity).HasColumnName("target_qty").HasColumnType("numeric(18,4)");
        builder.Property(x => x.TargetUomId).HasColumnName("target_uom_id");
        builder.Property(x => x.GoodQuantity).HasColumnName("good_qty").HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(x => x.RejectQuantity).HasColumnName("reject_qty").HasColumnType("numeric(18,4)").IsRequired();

        // --- Project commitment (the "order" lives here, never as master data; §2.5.4) -------------
        builder.Property(x => x.Deliverable).HasColumnName("deliverable").HasMaxLength(512);
        builder.Property(x => x.DeliverableItemId).HasColumnName("deliverable_item_id");
        builder.Property(x => x.CustomerId).HasColumnName("customer_id");
        builder.Property(x => x.CommittedDate).HasColumnName("committed_date");
        builder.Property(x => x.ContractRef).HasColumnName("contract_ref").HasMaxLength(128);
        builder.Property(x => x.AcceptanceAt).HasColumnName("acceptance_at");

        // --- Physical scope — LOGICAL references to config.* (§1.9) --------------------------------
        builder.Property(x => x.SiteId).HasColumnName("site_id");
        builder.Property(x => x.AreaId).HasColumnName("area_id");
        builder.Property(x => x.LineId).HasColumnName("line_id");
        builder.Property(x => x.WorkCenterId).HasColumnName("work_center_id");

        // --- Management and progress ---------------------------------------------------------------
        builder.Property(x => x.OwnerPersonId).HasColumnName("owner_person_id");
        builder.Property(x => x.Priority).HasColumnName("priority").IsRequired();
        builder.Property(x => x.ProgressPct).HasColumnName("progress_pct").HasColumnType("numeric(5,2)").IsRequired();
        builder.Property(x => x.ProgressMethod).HasColumnName("progress_method").HasMaxLength(32).IsRequired();
        builder.Property(x => x.ActualStartAt).HasColumnName("actual_start_at");
        builder.Property(x => x.ActualEndAt).HasColumnName("actual_end_at");

        builder.Property(x => x.CloseKind)
            .HasColumnName("close_kind")
            .HasConversion(ExecutionConfigurationExtensions.CloseKindConverter)
            .HasMaxLength(16);

        builder.Property(x => x.CloseReason).HasColumnName("close_reason").HasMaxLength(512);

        // Standard audit block (§1.3): created_by / updated_by / deleted_by are LOGICAL references to the
        // global identity (Control Plane), uuid without a foreign key (§1.9).
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        // Soft-delete (§1.4): deleted_at IS NULL means the row is live.
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.HasQueryFilter(x => x.DeletedAt == null);

        // The run owns its graph: task runs, consumptions and evidence travel with it and die with it.
        builder.HasMany(x => x.TaskRuns)
            .WithOne()
            .HasForeignKey(run => run.ExecutionId)
            .HasConstraintName("fk_task_runs_exec")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.InputConsumptions)
            .WithOne()
            .HasForeignKey(consumption => consumption.ExecutionId)
            .HasConstraintName("fk_ic_exec")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Evidence)
            .WithOne()
            .HasForeignKey(evidence => evidence.ExecutionId)
            .HasConstraintName("fk_ev_exec")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(Domain.Execution.TaskRuns))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Domain.Execution.InputConsumptions))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Domain.Execution.Evidence))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(x => x.SupportsOee);
        builder.Ignore(x => x.IsActive);
        builder.Ignore(x => x.WorkedTimeSeconds);
        builder.Ignore(x => x.DomainEvents);

        // The execution code is the natural key of the run within the tenant (unique among live rows).
        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName("ux_exec_code")
            .HasFilter(ExecutionConfigurationExtensions.LiveRowsFilter);

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("ix_exec_status")
            .HasFilter(ExecutionConfigurationExtensions.LiveRowsFilter);

        builder.HasIndex(x => new { x.Flavor, x.Status })
            .HasDatabaseName("ix_exec_flavor_status")
            .HasFilter(ExecutionConfigurationExtensions.LiveRowsFilter);

        builder.HasIndex(x => x.ProcessVersionId).HasDatabaseName("ix_exec_version");

        builder.HasIndex(x => x.CustomerId)
            .HasDatabaseName("ix_exec_customer")
            .HasFilter("customer_id IS NOT NULL");

        // The committed-date backlog is a project-only read: the partial index mirrors the DDL.
        builder.HasIndex(x => x.CommittedDate)
            .HasDatabaseName("ix_exec_committed")
            .HasFilter($"flavor = 'project' AND {ExecutionConfigurationExtensions.LiveRowsFilter}");

        builder.HasIndex(x => new { x.TriggerRefKind, x.TriggerRefId }).HasDatabaseName("ix_exec_trigger");
    }
}
