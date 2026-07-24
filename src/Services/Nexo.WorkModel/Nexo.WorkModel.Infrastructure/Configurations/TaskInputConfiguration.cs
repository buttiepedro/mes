using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexo.WorkModel.Domain;

namespace Nexo.WorkModel.Infrastructure.Configurations;

/// <summary>
/// <c>work.task_inputs</c> — the standard consumption declared by a task. A child of the task (which is
/// itself a child of the version): the <c>tasks -&gt; task_inputs</c> relationship (owning side, cascade
/// delete) is declared on <see cref="WorkTaskConfiguration"/>.
/// </summary>
/// <remarks>
/// <c>item_id</c> points at <c>master.items</c> and <c>uom_id</c> at <c>master.uom</c>, both owned by
/// Nexo.MasterData: they are mapped as <b>logical references without a foreign key</b> (§1.9), so the two
/// bounded contexts keep independent migration histories even sharing one physical database.
/// <c>process_version_id</c> is denormalized to keep every row of the graph anchored to a single version
/// (G4).
/// </remarks>
public sealed class TaskInputConfiguration : IEntityTypeConfiguration<TaskInput>
{
    public void Configure(EntityTypeBuilder<TaskInput> builder)
    {
        builder.ToTable("task_inputs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.TaskId).HasColumnName("task_id").IsRequired();
        builder.Property(x => x.ProcessVersionId).HasColumnName("process_version_id").IsRequired();

        // LOGICAL references to master.* — uuid WITHOUT a foreign key (§1.9).
        builder.Property(x => x.ItemId).HasColumnName("item_id").IsRequired();
        builder.Property(x => x.UomId).HasColumnName("uom_id").IsRequired();

        builder.Property(x => x.Quantity)
            .HasColumnName("quantity")
            .HasColumnType("numeric(18,6)")
            .IsRequired();

        builder.Property(x => x.Basis)
            .HasColumnName("basis")
            .HasConversion(WorkModelConfigurationExtensions.InputBasisConverter)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.Kind)
            .HasColumnName("kind")
            .HasConversion(WorkModelConfigurationExtensions.InputKindConverter)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.TolerancePct)
            .HasColumnName("tolerance_pct")
            .HasColumnType("numeric(9,4)");

        builder.Property(x => x.IsBlocking).HasColumnName("is_blocking").IsRequired();
        builder.Property(x => x.RequiresTraceability).HasColumnName("requires_traceability").IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.HasQueryFilter(x => x.DeletedAt == null);

        // The same item is never declared twice as an input of the same task (mirrors the aggregate rule).
        builder.HasIndex(x => new { x.TaskId, x.ItemId })
            .IsUnique()
            .HasDatabaseName("ux_task_inputs_task_item")
            .HasFilter(WorkModelConfigurationExtensions.LiveRowsFilter);

        builder.HasIndex(x => x.ItemId).HasDatabaseName("ix_task_inputs_item_id");
    }
}
