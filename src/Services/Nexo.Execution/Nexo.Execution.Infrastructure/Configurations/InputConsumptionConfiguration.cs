using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexo.Execution.Domain;

namespace Nexo.Execution.Infrastructure.Configurations;

/// <summary>
/// <c>execution.input_consumptions</c> (§2.7.3) — a real input consumption. A child of the
/// <see cref="Execution"/> aggregate (the owning <c>executions → input_consumptions</c> relationship,
/// cascade delete, is declared on <see cref="ExecutionConfiguration"/>).
/// </summary>
/// <remarks>
/// <b>No cost (MOD-17):</b> quantity, unit and (optionally) batch — never valuation, rate or cost.
/// <c>item_id</c> / <c>uom_id</c> → <c>master.*</c>, <c>task_input_id</c> → <c>work.task_inputs</c>,
/// <c>batch_id</c> / <c>serial_id</c> → <c>trace.*</c> and <c>person_id</c> → <c>master.people</c> are all
/// mapped as plain <c>uuid</c> columns — logical references without a physical foreign key (§1.9).
/// <c>task_run_id</c> points at a sibling row of the same aggregate; it is kept as a plain column too, so
/// the run's single cascade from the execution never becomes a redundant multi-path delete.
/// </remarks>
public sealed class InputConsumptionConfiguration : IEntityTypeConfiguration<InputConsumption>
{
    public void Configure(EntityTypeBuilder<InputConsumption> builder)
    {
        builder.ToTable("input_consumptions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.ExecutionId).HasColumnName("execution_id").IsRequired();
        builder.Property(x => x.TaskRunId).HasColumnName("task_run_id");
        builder.Property(x => x.TaskInputId).HasColumnName("task_input_id");

        builder.Property(x => x.ItemId).HasColumnName("item_id").IsRequired();

        builder.Property(x => x.Quantity)
            .HasColumnName("qty")
            .HasColumnType("numeric(18,4)")
            .IsRequired();

        builder.Property(x => x.UomId).HasColumnName("uom_id").IsRequired();

        builder.Property(x => x.PlannedQuantity).HasColumnName("planned_qty").HasColumnType("numeric(18,4)");

        builder.Property(x => x.Method)
            .HasColumnName("method")
            .HasConversion(ExecutionConfigurationExtensions.ConsumptionMethodConverter)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.BatchId).HasColumnName("batch_id");
        builder.Property(x => x.SerialId).HasColumnName("serial_id");
        builder.Property(x => x.PersonId).HasColumnName("person_id");
        builder.Property(x => x.RecordedAt).HasColumnName("recorded_at").IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.HasQueryFilter(x => x.DeletedAt == null);

        builder.HasIndex(x => x.ExecutionId).HasDatabaseName("ix_ic_exec");
        builder.HasIndex(x => x.TaskRunId).HasDatabaseName("ix_ic_run");

        builder.HasIndex(x => new { x.ItemId, x.RecordedAt })
            .HasDatabaseName("ix_ic_item_time")
            .IsDescending(false, true);

        builder.HasIndex(x => x.BatchId)
            .HasDatabaseName("ix_ic_batch")
            .HasFilter("batch_id IS NOT NULL");
    }
}
