using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nexo.WorkModel.Domain;

namespace Nexo.WorkModel.Infrastructure.Configurations;

/// <summary>
/// <c>work.task_dependencies</c> — one precedence of the DAG (predecessor, successor, kind, lag). A
/// child of the version aggregate: the <c>process_versions -&gt; task_dependencies</c> relationship
/// (owning side, cascade delete) is declared on <see cref="ProcessVersionConfiguration"/>.
/// </summary>
/// <remarks>
/// <c>predecessor_task_id</c> and <c>successor_task_id</c> point at <c>work.tasks</c> but are mapped as
/// plain <c>uuid</c> columns <b>without a foreign key</b>: both ends and the row itself all cascade from
/// the same version, and declaring physical FKs to the tasks would introduce redundant cascade paths.
/// The "both ends belong to this version" invariant (G4) is guaranteed by the denormalized
/// <c>process_version_id</c>, and acyclicity is a property of the whole graph enforced by
/// <see cref="ProcessVersion.SetGraph"/>, never of a single row.
/// </remarks>
public sealed class TaskDependencyConfiguration : IEntityTypeConfiguration<TaskDependency>
{
    public void Configure(EntityTypeBuilder<TaskDependency> builder)
    {
        builder.ToTable("task_dependencies");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(x => x.ProcessVersionId).HasColumnName("process_version_id").IsRequired();

        // Logical references to work.tasks — uuid without a foreign key (see remarks).
        builder.Property(x => x.PredecessorTaskId).HasColumnName("predecessor_task_id").IsRequired();
        builder.Property(x => x.SuccessorTaskId).HasColumnName("successor_task_id").IsRequired();

        builder.Property(x => x.Type)
            .HasColumnName("type")
            .HasConversion(WorkModelConfigurationExtensions.DependencyTypeConverter)
            .HasMaxLength(4)
            .IsRequired();

        builder.Property(x => x.LagSeconds).HasColumnName("lag_sec").IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");
        builder.HasQueryFilter(x => x.DeletedAt == null);

        // The graph is not a multigraph: an edge (predecessor -> successor) is declared at most once.
        builder.HasIndex(x => new { x.ProcessVersionId, x.PredecessorTaskId, x.SuccessorTaskId })
            .IsUnique()
            .HasDatabaseName("ux_task_dep_edge")
            .HasFilter(WorkModelConfigurationExtensions.LiveRowsFilter);

        builder.HasIndex(x => x.PredecessorTaskId).HasDatabaseName("ix_task_dep_predecessor");
        builder.HasIndex(x => x.SuccessorTaskId).HasDatabaseName("ix_task_dep_successor");
    }
}
