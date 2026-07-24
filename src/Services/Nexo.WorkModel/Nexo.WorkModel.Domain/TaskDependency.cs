using Nexo.BuildingBlocks.Domain;

namespace Nexo.WorkModel.Domain;

/// <summary>
/// One precedence of the DAG (<c>work.task_dependencies</c>): predecessor, successor, kind and lag.
/// </summary>
/// <remarks>
/// MOD-18 ships the <b>full DAG</b> in the MVP: the kind may be <c>FS</c> (finish→start),
/// <c>SS</c> (start→start) or <c>FF</c> (finish→finish), and <see cref="LagSeconds"/> is the
/// mandatory delay (curing, setting). Negative lag stays deferred to V1 (G5).
/// <para>
/// <see cref="ProcessVersionId"/> is denormalized so both ends are provably anchored to the same
/// version (G4). Acyclicity is a property of the whole graph, never of a row: it is enforced by
/// <see cref="ProcessVersion.SetGraph"/> through <see cref="TaskGraph.FindCycle"/>.
/// </para>
/// </remarks>
public sealed class TaskDependency : Entity<Guid>
{
    // EF Core materialization constructor.
    private TaskDependency()
    {
    }

    private TaskDependency(
        Guid id,
        Guid processVersionId,
        Guid predecessorTaskId,
        Guid successorTaskId,
        DependencyType type,
        int lagSeconds)
        : base(id)
    {
        ProcessVersionId = processVersionId;
        PredecessorTaskId = predecessorTaskId;
        SuccessorTaskId = successorTaskId;
        Type = type;
        LagSeconds = lagSeconds;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    /// <summary>Denormalized: makes G4 verifiable (both ends belong to this version).</summary>
    public Guid ProcessVersionId { get; private set; }

    public Guid PredecessorTaskId { get; private set; }

    public Guid SuccessorTaskId { get; private set; }

    public DependencyType Type { get; private set; }

    /// <summary>Mandatory delay in seconds. Never negative in the MVP (G5).</summary>
    public int LagSeconds { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public Guid? DeletedBy { get; private set; }

    internal static TaskDependency Create(
        Guid processVersionId,
        Guid predecessorTaskId,
        Guid successorTaskId,
        DependencyType type,
        int lagSeconds)
        => new(UuidV7.NewGuid(), processVersionId, predecessorTaskId, successorTaskId, type, lagSeconds);
}
