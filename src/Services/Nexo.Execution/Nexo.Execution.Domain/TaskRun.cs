using Nexo.BuildingBlocks.Domain;

namespace Nexo.Execution.Domain;

/// <summary>
/// One incoming precedence of a <see cref="TaskRun"/>, frozen from the process version's DAG. The
/// predecessor is referenced by its <b>work task id</b> (§1.9). Value object owned by the task run.
/// </summary>
public sealed record TaskRunPrecedence(Guid PredecessorTaskId, DependencyType Type, int LagSeconds);

/// <summary>
/// An instantiated task (<c>execution.task_runs</c>, §2.7.2): the <b>unit of imputation</b>. State,
/// assignment (role→person, resolved here and not in the template), real time by component
/// (setup/exec/wait/control/closing), progress, milestone and block. Child entity of the
/// <see cref="Execution"/> aggregate — it is created and transitioned only through the root, so the DAG
/// and closing invariants live in one place.
/// </summary>
/// <remarks>
/// The policy fields it carries (<see cref="Obligation"/>, <see cref="RequiredEvidenceKind"/>,
/// <see cref="MinEvidenceCount"/>, <see cref="StandardDurationSeconds"/>) are <b>frozen copies</b> from the
/// task definition of the snapshot, so the run enforces its own close/skip/evidence rules without ever
/// querying Work Model. References to <c>work.*</c>/<c>config.*</c>/<c>master.*</c> are uuid without a
/// physical foreign key. <b>No cost (MOD-17).</b>
/// </remarks>
public sealed class TaskRun : Entity<Guid>
{
    private readonly List<TaskRunPrecedence> _precedences = new();

    // EF Core materialization constructor.
    private TaskRun()
    {
    }

    private TaskRun(
        Guid id,
        Guid executionId,
        TaskSnapshot task,
        IEnumerable<TaskRunPrecedence> precedences,
        DateTimeOffset? milestoneCommittedDate)
        : base(id)
    {
        ExecutionId = executionId;
        TaskId = task.TaskId;
        Occurrence = 1;
        IsAdHoc = false;
        Name = task.Name;
        Code = task.Code;
        Status = TaskRunStatus.Pending;
        AssignedRoleId = task.ResponsibleRoleId;
        AssignedPersonId = task.SuggestedPersonId;
        AssignmentMode = AssignmentMode.Individual;
        StandardDurationSeconds = task.StandardDurationSeconds;
        EstimatedDurationSeconds = task.EstimatedDurationSeconds;
        ProgressWeight = task.ProgressWeight;
        Obligation = task.Obligation;
        IsMilestone = task.IsMilestone;
        MilestoneCommittedDate = milestoneCommittedDate;
        RequiredEvidenceKind = task.RequiredEvidenceKind;
        MinEvidenceCount = task.MinEvidenceCount < 0 ? (short)0 : task.MinEvidenceCount;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
        _precedences.AddRange(precedences);
    }

    public Guid ExecutionId { get; private set; }

    /// <summary>The frozen task of the version (<c>work.tasks</c>). Logical reference — never <c>null</c> in this slice (no ad-hoc).</summary>
    public Guid? TaskId { get; private set; }

    /// <summary>Occurrence N of a repeatable task (CB10). Fixed to 1 in this slice.</summary>
    public short Occurrence { get; private set; }

    public bool IsAdHoc { get; private set; }

    public string? Name { get; private set; }

    /// <summary>Natural code of the task inside the version ('T5'), kept for readable diagnostics.</summary>
    public string? Code { get; private set; }

    public TaskRunStatus Status { get; private set; }

    /// <summary>Logical reference to <c>config.roles</c> — no physical foreign key.</summary>
    public Guid? AssignedRoleId { get; private set; }

    /// <summary>Logical reference to <c>master.people</c> — resolved here, not in the template.</summary>
    public Guid? AssignedPersonId { get; private set; }

    public AssignmentMode AssignmentMode { get; private set; }

    public Guid? WorkCenterId { get; private set; }

    public Guid? ShiftId { get; private set; }

    /// <summary>Standard duration inherited (frozen) from the definition.</summary>
    public decimal? StandardDurationSeconds { get; private set; }

    public decimal? EstimatedDurationSeconds { get; private set; }

    /// <summary>Explicit progress weight (0–100) frozen from the definition; falls back to the standard duration.</summary>
    public decimal? ProgressWeight { get; private set; }

    public DateTimeOffset? ActualStartAt { get; private set; }

    public DateTimeOffset? ActualEndAt { get; private set; }

    // Real time by component, same canonical decomposition as the standard (work-model.md §3.5).
    public long ActualSetupSeconds { get; private set; }

    public long ActualExecSeconds { get; private set; }

    /// <summary>Technical wait: NOT idle time (CB18).</summary>
    public long ActualWaitSeconds { get; private set; }

    public long ActualControlSeconds { get; private set; }

    public long ActualClosingSeconds { get; private set; }

    /// <summary>Sum of the real components (mirrors the stored generated column of the DDL).</summary>
    public long ActualTotalSeconds =>
        ActualSetupSeconds + ActualExecSeconds + ActualWaitSeconds + ActualControlSeconds + ActualClosingSeconds;

    public decimal ProgressPct { get; private set; }

    public ProgressMethod? ProgressMethod { get; private set; }

    public decimal? ProducedQuantity { get; private set; }

    public decimal? TargetQuantity { get; private set; }

    public bool IsOnCriticalPath { get; private set; }

    public bool IsMilestone { get; private set; }

    /// <summary>The commitment is of the concrete run, not of the template.</summary>
    public DateTimeOffset? MilestoneCommittedDate { get; private set; }

    public DateTimeOffset? MilestoneReachedAt { get; private set; }

    public TaskObligation Obligation { get; private set; }

    /// <summary>Frozen evidence requirement (one kind + a minimum count) used to gate completion (E11).</summary>
    public EvidenceKind? RequiredEvidenceKind { get; private set; }

    public short MinEvidenceCount { get; private set; }

    public Guid? BlockedReasonCodeId { get; private set; }

    public DateTimeOffset? BlockedAt { get; private set; }

    public bool IsForcedClose { get; private set; }

    public string? SkipReason { get; private set; }

    public string? CloseReason { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public Guid? DeletedBy { get; private set; }

    public IReadOnlyCollection<TaskRunPrecedence> Precedences => _precedences;

    public bool IsMandatory => Obligation == TaskObligation.Mandatory;

    public bool HasStarted => ActualStartAt is not null;

    /// <summary><c>completed</c> and <c>skipped</c> are the admitted terminal states for the DAG (E6).</summary>
    public bool IsTerminal => Status is TaskRunStatus.Completed or TaskRunStatus.Skipped
        or TaskRunStatus.Cancelled or TaskRunStatus.Rejected;

    public bool IsFinished => Status == TaskRunStatus.Completed;

    internal static TaskRun Instantiate(
        Guid executionId,
        TaskSnapshot task,
        IEnumerable<TaskRunPrecedence> precedences,
        DateTimeOffset? milestoneCommittedDate)
        => new(UuidV7.NewGuid(), executionId, task, precedences, milestoneCommittedDate);

    /// <summary>Promotes a <see cref="TaskRunStatus.Pending"/> run to <see cref="TaskRunStatus.Ready"/>.</summary>
    internal bool MarkReady()
    {
        if (Status != TaskRunStatus.Pending)
        {
            return false;
        }

        Status = TaskRunStatus.Ready;
        Touch();
        return true;
    }

    /// <summary>Assigns the run to a person/crew. The role→person resolution happens here (CB19).</summary>
    internal Result Assign(Guid? personId, Guid? roleId, AssignmentMode mode)
    {
        if (IsTerminal)
        {
            return Result.Failure(ExecutionErrors.TaskAlreadyTerminalConflict(Describe(), Status));
        }

        if (personId is not null)
        {
            AssignedPersonId = personId;
        }

        if (roleId is not null)
        {
            AssignedRoleId = roleId;
        }

        AssignmentMode = mode;

        if (Status == TaskRunStatus.Ready)
        {
            Status = TaskRunStatus.Assigned;
        }

        Touch();
        return Result.Success();
    }

    /// <summary>Starts the run; the real clock opens. Requires it to be enabled (ready/assigned).</summary>
    internal Result Start(Guid? operatorId)
    {
        if (IsTerminal)
        {
            return Result.Failure(ExecutionErrors.TaskAlreadyTerminalConflict(Describe(), Status));
        }

        if (Status is not (TaskRunStatus.Ready or TaskRunStatus.Assigned))
        {
            return Result.Failure(ExecutionErrors.TaskNotInProgressConflict(Describe(), Status));
        }

        if (operatorId is not null)
        {
            AssignedPersonId = operatorId;
        }

        Status = TaskRunStatus.InProgress;
        ActualStartAt = DateTimeOffset.UtcNow;
        Touch();
        return Result.Success();
    }

    /// <summary>Declares partial progress (§ never above 100, never negative).</summary>
    internal Result ReportProgress(decimal progressPct, ProgressMethod method, decimal? quantity, decimal? targetQuantity)
    {
        if (Status != TaskRunStatus.InProgress)
        {
            return Result.Failure(ExecutionErrors.TaskNotInProgressConflict(Describe(), Status));
        }

        if (progressPct is < 0m or > 100m)
        {
            return Result.Failure(ExecutionErrors.ProgressOutOfRangeInvalid);
        }

        if (quantity is < 0m)
        {
            return Result.Failure(ExecutionErrors.NegativeQuantityInvalid);
        }

        ProgressPct = progressPct;
        ProgressMethod = method;

        if (quantity is not null)
        {
            ProducedQuantity = quantity;
        }

        if (targetQuantity is not null)
        {
            TargetQuantity = targetQuantity;
        }

        Touch();
        return Result.Success();
    }

    internal Result Block(BlockCause cause, Guid? reasonCodeId)
    {
        if (IsTerminal)
        {
            return Result.Failure(ExecutionErrors.TaskAlreadyTerminalConflict(Describe(), Status));
        }

        if (Status == TaskRunStatus.Blocked)
        {
            return Result.Success();
        }

        Status = TaskRunStatus.Blocked;
        BlockedReasonCodeId = reasonCodeId;
        BlockedAt = DateTimeOffset.UtcNow;
        Touch();
        return Result.Success();
    }

    internal Result<long> Unblock(string? resolution)
    {
        if (Status != TaskRunStatus.Blocked)
        {
            return Result<long>.Failure(ExecutionErrors.TaskNotBlockedConflict(Describe()));
        }

        var blockedDuration = BlockedAt is null
            ? 0L
            : (long)Math.Max(0d, (DateTimeOffset.UtcNow - BlockedAt.Value).TotalSeconds);

        // Returns to work: if it had started, back to in-progress; otherwise back to the ready queue.
        Status = HasStarted ? TaskRunStatus.InProgress : TaskRunStatus.Ready;
        BlockedAt = null;
        BlockedReasonCodeId = null;
        CloseReason = string.IsNullOrWhiteSpace(resolution) ? CloseReason : resolution.Trim();
        Touch();
        return Result<long>.Success(blockedDuration);
    }

    /// <summary>
    /// Marks the run completed. Cross-entity guards (predecessors, evidence) are enforced by the root
    /// before this is called; here only the local transition and clock closing happen.
    /// </summary>
    internal void Complete(bool force)
    {
        Status = TaskRunStatus.Completed;
        IsForcedClose = force;
        ProgressPct = 100m;
        ActualEndAt = DateTimeOffset.UtcNow;

        if (ActualStartAt is not null && ActualExecSeconds == 0)
        {
            ActualExecSeconds = (long)Math.Max(0d, (ActualEndAt.Value - ActualStartAt.Value).TotalSeconds);
        }

        if (IsMilestone)
        {
            MilestoneReachedAt = ActualEndAt;
        }

        Touch();
    }

    /// <summary>Marks the run skipped (out of the progress denominator).</summary>
    internal void Skip(string reason, Guid? authorizedBy)
    {
        Status = TaskRunStatus.Skipped;
        SkipReason = reason.Trim();
        CreatedBy ??= authorizedBy;
        ActualEndAt = DateTimeOffset.UtcNow;
        Touch();
    }

    /// <summary>Records the justification of a completion/close, without changing state.</summary>
    internal void SetCloseReason(string reason)
    {
        CloseReason = string.IsNullOrWhiteSpace(reason) ? CloseReason : reason.Trim();
        Touch();
    }

    /// <summary>Progress weight of the run: the explicit weight, or the standard duration, or an equal share.</summary>
    internal decimal ProgressWeightOrDuration()
    {
        if (ProgressWeight is decimal weight && weight >= 0m)
        {
            return weight;
        }

        if (StandardDurationSeconds is decimal duration && duration > 0m)
        {
            return duration;
        }

        return 1m;
    }

    /// <summary>Whether an incoming precedence is satisfied enough for this run to become ready/start.</summary>
    internal static bool SatisfiesStart(TaskRunPrecedence precedence, TaskRun predecessor) => precedence.Type switch
    {
        // finish→start: the predecessor must be in an admitted terminal state (E6).
        DependencyType.FS => predecessor.IsTerminal,
        // start→start: it is enough that the predecessor has started.
        DependencyType.SS => predecessor.HasStarted || predecessor.IsTerminal,
        // finish→finish: never gates the start.
        _ => true
    };

    /// <summary>Human-readable identifier for diagnostics.</summary>
    internal string Describe() => Code ?? Name ?? Id.ToString();

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}
