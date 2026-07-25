using Nexo.Execution.Domain;

// Top-level namespace on purpose: nesting under `Nexo.Execution` would make the identifier
// `Execution` bind to the namespace instead of the aggregate type (CS0118).
namespace ExecutionTests;

/// <summary>
/// Builders for the domain shapes an <see cref="Execution"/> is created from. Ids are ordinary
/// <see cref="Guid"/>s (test data); precedences reference tasks by the same id used to build them.
/// </summary>
internal static class ExecutionTestFactory
{
    public static readonly Guid RoleId = Guid.NewGuid();

    public static TaskSnapshot Task(
        Guid id,
        string code,
        TaskObligation obligation = TaskObligation.Mandatory,
        EvidenceKind? requiredEvidence = null,
        short minEvidence = 0,
        bool milestone = false)
        => new(
            TaskId: id,
            Code: code,
            Name: code,
            ResponsibleRoleId: RoleId,
            Obligation: obligation,
            IsMilestone: milestone,
            RequiredEvidenceKind: requiredEvidence,
            MinEvidenceCount: minEvidence);

    public static ExecutionTrigger Manual => new(TriggerKind.Manual);

    public static BatchTarget Target() => new(Guid.NewGuid(), 100m, Guid.NewGuid());

    public static ProjectCommitment Commitment()
        => new("Aluminium curtain wall", DateTimeOffset.UtcNow.AddDays(30), Guid.NewGuid());

    public static ProcessSnapshot Batch(IReadOnlyList<TaskSnapshot> tasks, params PrecedenceSnapshot[] edges)
        => new(Guid.NewGuid(), Guid.NewGuid(), "1", ExecutionFlavor.Batch, tasks, edges);

    public static ProcessSnapshot Project(IReadOnlyList<TaskSnapshot> tasks, params PrecedenceSnapshot[] edges)
        => new(Guid.NewGuid(), Guid.NewGuid(), "1", ExecutionFlavor.Project, tasks, edges);

    /// <summary>Finish→start edge (the default and most common precedence).</summary>
    public static PrecedenceSnapshot Fs(Guid predecessor, Guid successor)
        => new(predecessor, successor, DependencyType.FS, 0);

    /// <summary>Convenience: the task run materialized from the task with the given code.</summary>
    public static TaskRun Run(this Execution execution, string code)
        => execution.TaskRuns.Single(run => run.Code == code);
}
