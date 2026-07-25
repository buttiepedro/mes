using Nexo.BuildingBlocks.Domain;

namespace Nexo.Execution.Domain;

/// <summary>
/// The <b>Execution (Run)</b> (<c>execution.executions</c>, §2.7): the living instance of a frozen process
/// version and the <b>aggregate root</b> of a run. A single aggregate — and a single table skeleton —
/// serves the two flavours (E-decisions): a <see cref="ExecutionFlavor.Batch"/> carries a target
/// (product + quantity), a <see cref="ExecutionFlavor.Project"/> carries a commitment (deliverable,
/// committed date, customer) as <b>attributes of the run</b>, never as a catalogue of orders.
/// </summary>
/// <remarks>
/// <para>
/// <b>Instantiation from a snapshot (E1/E2).</b> Execution is a bounded context of its own and never reads
/// the <c>work</c> schema. <see cref="Create"/> receives a <see cref="ProcessSnapshot"/> of the published
/// version and materializes the <see cref="TaskRun"/> graph and its frozen precedences from it. In the real
/// integration that snapshot is obtained over gRPC from Work Model
/// (<c>ProcessCatalog.GetPublishedVersion</c>, docs/design/04-service-contracts.md §2.6/§2.7) — that call is
/// <b>pending</b> and out of scope here.
/// </para>
/// <para>
/// <b>MVP slice.</b> The separate <c>:schedule</c>/<c>:release</c> steps of the contract are folded into
/// creation, so a run is born <see cref="ExecutionStatus.Released"/> with its start nodes enabled. It turns
/// <see cref="ExecutionStatus.InProgress"/> on the first started task. The DAG scheduler (wall-clock lag
/// timers, critical path) and the imputation inbox are separate slices, deferred here.
/// </para>
/// <para>
/// <b>No cost (MOD-17).</b> No valuation, rate or cost anywhere. <b>E23:</b> OEE does not apply to the
/// project flavour — see <see cref="SupportsOee"/>. References to <c>master.*</c>/<c>config.*</c>/<c>work.*</c>
/// are uuid <b>without a physical foreign key</b> (§1.9).
/// </para>
/// </remarks>
public sealed class Execution : AggregateRoot<Guid>
{
    private readonly List<TaskRun> _taskRuns = new();
    private readonly List<InputConsumption> _inputConsumptions = new();
    private readonly List<Evidence> _evidence = new();

    // EF Core materialization constructor.
    private Execution() => Code = string.Empty;

    private Execution(
        Guid id,
        string code,
        ProcessSnapshot snapshot,
        ExecutionTrigger trigger,
        BatchTarget? target,
        ProjectCommitment? commitment,
        ExecutionScope? scope,
        Guid? ownerPersonId,
        int priority)
        : base(id)
    {
        Code = code.Trim();
        ProcessId = snapshot.ProcessId;
        ProcessVersionId = snapshot.ProcessVersionId;
        VersionNo = snapshot.VersionNo;
        Flavor = snapshot.Flavor;
        Status = ExecutionStatus.Released;

        TriggerKind = trigger.Kind;
        TriggerRefKind = Normalize(trigger.RefKind);
        TriggerRefId = trigger.RefId;
        TriggerExternalRef = Normalize(trigger.ExternalRef);

        if (target is not null)
        {
            TargetItemId = target.ItemId;
            TargetQuantity = target.Quantity;
            TargetUomId = target.UomId;
        }

        if (commitment is not null)
        {
            Deliverable = commitment.Deliverable.Trim();
            DeliverableItemId = commitment.DeliverableItemId;
            CustomerId = commitment.CustomerId;
            CommittedDate = commitment.CommittedDate;
            ContractRef = Normalize(commitment.ContractRef);
        }

        SiteId = scope?.SiteId;
        AreaId = scope?.AreaId;
        LineId = scope?.LineId;
        WorkCenterId = scope?.WorkCenterId;

        OwnerPersonId = ownerPersonId;
        Priority = priority;
        ProgressMethod = "weighted_standard_time";
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public string Code { get; private set; }

    /// <summary>The frozen template (E1/E2): logical references to <c>work.processes</c>/<c>work.process_versions</c>.</summary>
    public Guid ProcessId { get; private set; }

    public Guid ProcessVersionId { get; private set; }

    public string? VersionNo { get; private set; }

    public ExecutionFlavor Flavor { get; private set; }

    public ExecutionStatus Status { get; private set; }

    // --- Trigger (polymorphic, may be external) -----------------------------------------------

    public TriggerKind TriggerKind { get; private set; }

    public string? TriggerRefKind { get; private set; }

    public Guid? TriggerRefId { get; private set; }

    public string? TriggerExternalRef { get; private set; }

    // --- Batch objective ----------------------------------------------------------------------

    public Guid? TargetItemId { get; private set; }

    public decimal? TargetQuantity { get; private set; }

    public Guid? TargetUomId { get; private set; }

    public decimal GoodQuantity { get; private set; }

    public decimal RejectQuantity { get; private set; }

    // --- Project commitment (the "order" lives here) ------------------------------------------

    public string? Deliverable { get; private set; }

    public Guid? DeliverableItemId { get; private set; }

    public Guid? CustomerId { get; private set; }

    public DateTimeOffset? CommittedDate { get; private set; }

    public string? ContractRef { get; private set; }

    public DateTimeOffset? AcceptanceAt { get; private set; }

    // --- Physical scope -----------------------------------------------------------------------

    public Guid? SiteId { get; private set; }

    public Guid? AreaId { get; private set; }

    public Guid? LineId { get; private set; }

    public Guid? WorkCenterId { get; private set; }

    // --- Management and progress --------------------------------------------------------------

    public Guid? OwnerPersonId { get; private set; }

    public int Priority { get; private set; }

    public decimal ProgressPct { get; private set; }

    public string ProgressMethod { get; private set; } = "weighted_standard_time";

    public DateTimeOffset? ActualStartAt { get; private set; }

    public DateTimeOffset? ActualEndAt { get; private set; }

    public CloseKind? CloseKind { get; private set; }

    public string? CloseReason { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public Guid? DeletedBy { get; private set; }

    public IReadOnlyCollection<TaskRun> TaskRuns => _taskRuns;

    public IReadOnlyCollection<InputConsumption> InputConsumptions => _inputConsumptions;

    public IReadOnlyCollection<Evidence> Evidence => _evidence;

    /// <summary>E23: OEE is only meaningful for the batch flavour; consumers hide it for projects.</summary>
    public bool SupportsOee => Flavor == ExecutionFlavor.Batch;

    /// <summary>An open run admits operational changes; closed/cancelled/archived do not.</summary>
    public bool IsActive => Status is not (ExecutionStatus.Closed or ExecutionStatus.Cancelled or ExecutionStatus.Archived);

    /// <summary>Sum of the real time imputed to every task run.</summary>
    public long WorkedTimeSeconds => _taskRuns.Sum(run => run.ActualTotalSeconds);

    /// <summary>
    /// Creates a run from a published-version snapshot: materializes its task runs and frozen DAG, enables
    /// its start nodes and raises <see cref="ExecutionCreatedDomainEvent"/> (plus a
    /// <see cref="TaskRunEnabledDomainEvent"/> per enabled node).
    /// </summary>
    public static Result<Execution> Create(
        string code,
        ProcessSnapshot snapshot,
        ExecutionTrigger trigger,
        BatchTarget? target = null,
        ProjectCommitment? commitment = null,
        ExecutionScope? scope = null,
        Guid? ownerPersonId = null,
        int priority = 0)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result<Execution>.Failure(ExecutionErrors.CodeRequiredInvalid);
        }

        if (snapshot.Tasks.Count == 0)
        {
            return Result<Execution>.Failure(ExecutionErrors.SnapshotEmptyInvalid);
        }

        if (snapshot.Flavor == ExecutionFlavor.Batch)
        {
            if (target is null || target.ItemId == Guid.Empty || target.Quantity <= 0m)
            {
                return Result<Execution>.Failure(ExecutionErrors.BatchTargetRequiredInvalid);
            }
        }
        else
        {
            // W15: a project never declares a target output quantity.
            if (target is not null)
            {
                return Result<Execution>.Failure(ExecutionErrors.ProjectTargetNotAllowedInvalid);
            }

            if (commitment is null
                || string.IsNullOrWhiteSpace(commitment.Deliverable)
                || commitment.CommittedDate == default)
            {
                return Result<Execution>.Failure(ExecutionErrors.ProjectCommitmentRequiredInvalid);
            }
        }

        var execution = new Execution(
            UuidV7.NewGuid(),
            code,
            snapshot,
            trigger,
            snapshot.Flavor == ExecutionFlavor.Batch ? target : null,
            snapshot.Flavor == ExecutionFlavor.Project ? commitment : null,
            scope,
            ownerPersonId,
            priority);

        execution.MaterializeTaskRuns(snapshot, commitment);

        execution.Raise(new ExecutionCreatedDomainEvent(
            execution.Id,
            execution.Code,
            execution.Flavor,
            execution.ProcessId,
            execution.ProcessVersionId,
            execution.VersionNo ?? string.Empty,
            execution._taskRuns.Count));

        // Enable the start nodes of the DAG (raises TaskRunEnabled for each).
        execution.RecomputeReadiness();

        return Result<Execution>.Success(execution);
    }

    public TaskRun? FindTaskRun(Guid taskRunId) => _taskRuns.FirstOrDefault(run => run.Id == taskRunId);

    /// <summary>Assigns a task run to a person/crew (the operator's self-assignment from the tablet).</summary>
    public Result TakeTask(Guid taskRunId, Guid? personId, Guid? roleId, AssignmentMode mode)
    {
        var guard = EnsureActive();
        if (guard.IsFailure)
        {
            return guard;
        }

        var run = FindTaskRun(taskRunId);
        if (run is null)
        {
            return Result.Failure(ExecutionErrors.TaskRunNotFound(taskRunId.ToString()));
        }

        var assigned = run.Assign(personId, roleId, mode);
        if (assigned.IsFailure)
        {
            return assigned;
        }

        Raise(new TaskRunAssignedDomainEvent(Id, run.Id, run.AssignmentMode, run.AssignedPersonId, run.AssignedRoleId));
        Touch();
        return Result.Success();
    }

    /// <summary>Starts a task run: checks it is enabled (E6/E7), opens the clock and starts the run if it is the first.</summary>
    public Result StartTask(Guid taskRunId, Guid? operatorId)
    {
        var guard = EnsureActive();
        if (guard.IsFailure)
        {
            return guard;
        }

        var run = FindTaskRun(taskRunId);
        if (run is null)
        {
            return Result.Failure(ExecutionErrors.TaskRunNotFound(taskRunId.ToString()));
        }

        // A pending run has not been enabled: report which predecessors are still holding it back.
        if (run.Status == TaskRunStatus.Pending)
        {
            var pending = UnsatisfiedPredecessors(run);
            return Result.Failure(ExecutionErrors.TaskNotReadyConflict(run.Describe(), pending));
        }

        var started = run.Start(operatorId);
        if (started.IsFailure)
        {
            return started;
        }

        // The run starting is what starts the execution (E-clock).
        if (Status is ExecutionStatus.Released or ExecutionStatus.Scheduled or ExecutionStatus.Reopened)
        {
            Status = ExecutionStatus.InProgress;
            ActualStartAt = DateTimeOffset.UtcNow;
            Raise(new ExecutionStartedDomainEvent(Id, ActualStartAt.Value, run.Id));
        }

        Raise(new TaskRunStartedDomainEvent(Id, run.Id, run.TaskId, run.ActualStartAt!.Value, run.AssignedPersonId));
        Touch();
        return Result.Success();
    }

    public Result ReportProgress(Guid taskRunId, ProgressMethod method, decimal progressPct, decimal? quantity, decimal? targetQuantity)
    {
        var guard = EnsureActive();
        if (guard.IsFailure)
        {
            return guard;
        }

        var run = FindTaskRun(taskRunId);
        if (run is null)
        {
            return Result.Failure(ExecutionErrors.TaskRunNotFound(taskRunId.ToString()));
        }

        var reported = run.ReportProgress(progressPct, method, quantity, targetQuantity);
        if (reported.IsFailure)
        {
            return reported;
        }

        RecalculateProgress();
        Raise(new TaskRunProgressReportedDomainEvent(Id, run.Id, run.ProgressPct, method, run.ProducedQuantity));
        Touch();
        return Result.Success();
    }

    public Result BlockTask(Guid taskRunId, BlockCause cause, Guid? reasonCodeId)
    {
        var guard = EnsureActive();
        if (guard.IsFailure)
        {
            return guard;
        }

        var run = FindTaskRun(taskRunId);
        if (run is null)
        {
            return Result.Failure(ExecutionErrors.TaskRunNotFound(taskRunId.ToString()));
        }

        var blocked = run.Block(cause, reasonCodeId);
        if (blocked.IsFailure)
        {
            return blocked;
        }

        Raise(new TaskRunBlockedDomainEvent(Id, run.Id, cause, reasonCodeId, run.BlockedAt ?? DateTimeOffset.UtcNow));
        Touch();
        return Result.Success();
    }

    public Result UnblockTask(Guid taskRunId, string? resolution)
    {
        var guard = EnsureActive();
        if (guard.IsFailure)
        {
            return guard;
        }

        var run = FindTaskRun(taskRunId);
        if (run is null)
        {
            return Result.Failure(ExecutionErrors.TaskRunNotFound(taskRunId.ToString()));
        }

        var unblocked = run.Unblock(resolution);
        if (unblocked.IsFailure)
        {
            return Result.Failure(unblocked.Error);
        }

        Raise(new TaskRunUnblockedDomainEvent(Id, run.Id, unblocked.Value, resolution));
        Touch();
        return Result.Success();
    }

    /// <summary>
    /// Completes a task run. Enforces the DAG (finish→finish predecessors closed), the completion of any
    /// preceding start-precedences and the mandatory evidence (E11) — unless <paramref name="force"/> is set
    /// (a forced close needs the admin permission, checked at the API and marked as an exception here, E19).
    /// Enables the successors and recomputes progress.
    /// </summary>
    public Result CompleteTask(Guid taskRunId, bool force, string? reason)
    {
        var guard = EnsureActive();
        if (guard.IsFailure)
        {
            return guard;
        }

        var run = FindTaskRun(taskRunId);
        if (run is null)
        {
            return Result.Failure(ExecutionErrors.TaskRunNotFound(taskRunId.ToString()));
        }

        if (run.IsTerminal)
        {
            return Result.Failure(ExecutionErrors.TaskAlreadyTerminalConflict(run.Describe(), run.Status));
        }

        if (!force)
        {
            // Cannot complete a task the DAG has not enabled yet.
            if (run.Status == TaskRunStatus.Pending)
            {
                return Result.Failure(ExecutionErrors.TaskNotReadyConflict(run.Describe(), UnsatisfiedPredecessors(run)));
            }

            // finish→finish predecessors must be finished first.
            var openFinish = OpenFinishPredecessors(run);
            if (openFinish.Count > 0)
            {
                return Result.Failure(ExecutionErrors.FinishPredecessorsOpenConflict(run.Describe(), openFinish));
            }

            // E11: mandatory evidence must be present (pending is admitted, offline-first).
            if (run.RequiredEvidenceKind is { } requiredKind && run.MinEvidenceCount > 0)
            {
                var present = _evidence.Count(e => e.TaskRunId == run.Id && e.Kind == requiredKind);
                if (present < run.MinEvidenceCount)
                {
                    return Result.Failure(ExecutionErrors.MandatoryEvidenceMissingConflict(
                        run.Describe(), requiredKind, run.MinEvidenceCount, present));
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(reason))
        {
            run.SetCloseReason(reason);
        }

        run.Complete(force);

        Raise(new TaskRunCompletedDomainEvent(Id, run.Id, run.TaskId, force, run.IsMilestone, run.ActualTotalSeconds));

        if (run.IsMilestone)
        {
            Raise(new ExecutionMilestoneReachedDomainEvent(
                Id, run.Id, run.MilestoneCommittedDate, run.MilestoneReachedAt ?? DateTimeOffset.UtcNow));
        }

        RecomputeReadiness();
        RecalculateProgress();
        Touch();
        return Result.Success();
    }

    public Result SkipTask(Guid taskRunId, string reason, Guid? authorizedBy)
    {
        var guard = EnsureActive();
        if (guard.IsFailure)
        {
            return guard;
        }

        var run = FindTaskRun(taskRunId);
        if (run is null)
        {
            return Result.Failure(ExecutionErrors.TaskRunNotFound(taskRunId.ToString()));
        }

        if (run.IsTerminal)
        {
            return Result.Failure(ExecutionErrors.TaskAlreadyTerminalConflict(run.Describe(), run.Status));
        }

        // E18: a mandatory task can only be skipped with an authorization.
        if (run.IsMandatory && authorizedBy is null)
        {
            return Result.Failure(ExecutionErrors.MandatorySkipUnauthorizedConflict(run.Describe()));
        }

        run.Skip(reason, authorizedBy);

        Raise(new TaskRunSkippedDomainEvent(Id, run.Id, run.Obligation, reason.Trim(), authorizedBy));

        RecomputeReadiness();
        RecalculateProgress();
        Touch();
        return Result.Success();
    }

    /// <summary>Registers a real input consumption (no cost, MOD-17). Quantity strictly positive.</summary>
    public Result<InputConsumption> ConsumeInput(
        Guid? taskRunId,
        Guid itemId,
        decimal quantity,
        Guid uomId,
        ConsumptionMethod method,
        decimal? plannedQuantity = null,
        Guid? taskInputId = null,
        Guid? batchId = null,
        Guid? serialId = null,
        Guid? personId = null)
    {
        var guard = EnsureActive();
        if (guard.IsFailure)
        {
            return Result<InputConsumption>.Failure(guard.Error);
        }

        if (quantity <= 0m)
        {
            return Result<InputConsumption>.Failure(ExecutionErrors.ConsumptionQuantityInvalid);
        }

        if (taskRunId is not null && FindTaskRun(taskRunId.Value) is null)
        {
            return Result<InputConsumption>.Failure(ExecutionErrors.TaskRunNotFound(taskRunId.Value.ToString()));
        }

        var consumption = InputConsumption.Create(
            Id, taskRunId, taskInputId, itemId, quantity, uomId, plannedQuantity, method, batchId, serialId, personId);
        _inputConsumptions.Add(consumption);

        Raise(new ExecutionInputConsumedDomainEvent(Id, consumption.Id, taskRunId, itemId, quantity, uomId, method));
        Touch();
        return Result<InputConsumption>.Success(consumption);
    }

    /// <summary>Attaches (or materializes) a piece of evidence to a task run — cancels evidence debt.</summary>
    public Result<Evidence> AttachEvidence(
        Guid taskRunId,
        EvidenceKind kind,
        EvidenceStatus status,
        Guid? fileId = null,
        string? mediaRef = null,
        byte[]? contentHash = null,
        Guid? requirementId = null,
        Guid? capturedBy = null,
        string? caption = null)
    {
        var guard = EnsureActive();
        if (guard.IsFailure)
        {
            return Result<Evidence>.Failure(guard.Error);
        }

        var run = FindTaskRun(taskRunId);
        if (run is null)
        {
            return Result<Evidence>.Failure(ExecutionErrors.TaskRunNotFound(taskRunId.ToString()));
        }

        if (fileId is null && string.IsNullOrWhiteSpace(mediaRef))
        {
            return Result<Evidence>.Failure(ExecutionErrors.EvidencePayloadMissingInvalid);
        }

        var isMandatory = run.RequiredEvidenceKind is not null && run.MinEvidenceCount > 0;
        var satisfiesRequirement = run.RequiredEvidenceKind == kind;

        var evidence = Domain.Evidence.Create(
            Id, run.Id, kind, status, requirementId, fileId, mediaRef, contentHash, isMandatory, capturedBy, caption);
        _evidence.Add(evidence);

        Raise(new EvidenceAttachedDomainEvent(Id, run.Id, evidence.Id, kind, status, satisfiesRequirement));
        Touch();
        return Result<Evidence>.Success(evidence);
    }

    /// <summary>
    /// Closes the run. A normal close rejects a run with mandatory task runs still open (part of the
    /// close checklist); a <see cref="CloseKind.Forced"/> or <see cref="CloseKind.Partial"/> close overrides it.
    /// </summary>
    public Result Close(CloseKind kind, string? reason)
    {
        if (Status is ExecutionStatus.Closed or ExecutionStatus.Cancelled)
        {
            return Result.Failure(ExecutionErrors.ExecutionAlreadyClosedConflict);
        }

        if (kind is not (Domain.CloseKind.Forced or Domain.CloseKind.Partial))
        {
            var openMandatory = _taskRuns
                .Where(run => run.IsMandatory && !run.IsTerminal)
                .Select(run => run.Describe())
                .ToArray();

            if (openMandatory.Length > 0)
            {
                return Result.Failure(ExecutionErrors.MandatoryTasksOpenConflict(openMandatory));
            }
        }

        RecalculateProgress();
        CloseKind = kind;
        CloseReason = Normalize(reason);
        Status = ExecutionStatus.Closed;
        ActualEndAt = DateTimeOffset.UtcNow;

        Raise(new ExecutionClosedDomainEvent(Id, Flavor, kind, ProgressPct, WorkedTimeSeconds, CloseReason));
        Touch();
        return Result.Success();
    }

    /// <summary>Cancels the run; incurred time and consumption are preserved (never a destructive edit, E22).</summary>
    public Result Cancel(string reason)
    {
        if (Status is ExecutionStatus.Closed or ExecutionStatus.Cancelled)
        {
            return Result.Failure(ExecutionErrors.ExecutionAlreadyClosedConflict);
        }

        CloseKind = Domain.CloseKind.Cancelled;
        CloseReason = Normalize(reason);
        Status = ExecutionStatus.Cancelled;
        ActualEndAt = DateTimeOffset.UtcNow;

        Raise(new ExecutionCancelledDomainEvent(Id, CloseReason ?? string.Empty, WorkedTimeSeconds));
        Touch();
        return Result.Success();
    }

    // --- Internal helpers ---------------------------------------------------------------------

    private void MaterializeTaskRuns(ProcessSnapshot snapshot, ProjectCommitment? commitment)
    {
        var precedencesBySuccessor = snapshot.Precedences
            .GroupBy(edge => edge.SuccessorTaskId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(edge => new TaskRunPrecedence(edge.PredecessorTaskId, edge.Type, edge.LagSeconds)).ToArray());

        foreach (var task in snapshot.Tasks)
        {
            var precedences = precedencesBySuccessor.TryGetValue(task.TaskId, out var incoming)
                ? incoming
                : Array.Empty<TaskRunPrecedence>();

            var milestoneCommittedDate = snapshot.Flavor == ExecutionFlavor.Project && task.IsMilestone
                ? commitment?.CommittedDate
                : null;

            _taskRuns.Add(TaskRun.Instantiate(Id, task, precedences, milestoneCommittedDate));
        }
    }

    /// <summary>Enables every pending run whose incoming precedences are satisfied, raising the enable event.</summary>
    private void RecomputeReadiness()
    {
        var runsByTaskId = _taskRuns
            .Where(run => run.TaskId is not null)
            .ToDictionary(run => run.TaskId!.Value, run => run);

        foreach (var run in _taskRuns)
        {
            if (run.Status != TaskRunStatus.Pending)
            {
                continue;
            }

            if (!AllStartPrecedencesSatisfied(run, runsByTaskId))
            {
                continue;
            }

            if (run.MarkReady())
            {
                Raise(new TaskRunEnabledDomainEvent(
                    Id, run.Id, run.TaskId, run.AssignedRoleId ?? Guid.Empty, DateTimeOffset.UtcNow));
            }
        }
    }

    private bool AllStartPrecedencesSatisfied(TaskRun run, IReadOnlyDictionary<Guid, TaskRun> runsByTaskId)
    {
        foreach (var precedence in run.Precedences)
        {
            if (!runsByTaskId.TryGetValue(precedence.PredecessorTaskId, out var predecessor))
            {
                // An unknown predecessor cannot gate the run (defensive; the snapshot is internally consistent).
                continue;
            }

            if (!TaskRun.SatisfiesStart(precedence, predecessor))
            {
                return false;
            }
        }

        return true;
    }

    private IReadOnlyList<string> UnsatisfiedPredecessors(TaskRun run)
    {
        var runsByTaskId = _taskRuns
            .Where(r => r.TaskId is not null)
            .ToDictionary(r => r.TaskId!.Value, r => r);

        var pending = new List<string>();

        foreach (var precedence in run.Precedences)
        {
            if (runsByTaskId.TryGetValue(precedence.PredecessorTaskId, out var predecessor)
                && !TaskRun.SatisfiesStart(precedence, predecessor))
            {
                pending.Add(predecessor.Describe());
            }
        }

        return pending;
    }

    private IReadOnlyList<string> OpenFinishPredecessors(TaskRun run)
    {
        var runsByTaskId = _taskRuns
            .Where(r => r.TaskId is not null)
            .ToDictionary(r => r.TaskId!.Value, r => r);

        var open = new List<string>();

        foreach (var precedence in run.Precedences.Where(p => p.Type == DependencyType.FF))
        {
            if (runsByTaskId.TryGetValue(precedence.PredecessorTaskId, out var predecessor) && !predecessor.IsFinished)
            {
                open.Add(predecessor.Describe());
            }
        }

        return open;
    }

    /// <summary>
    /// Weighted progress of the run (progress method <c>weighted_standard_time</c>): each task run weighs its
    /// explicit progress weight, or its standard duration, or an equal share. Skipped runs leave the denominator.
    /// </summary>
    private void RecalculateProgress()
    {
        var contributing = _taskRuns.Where(run => run.Status != TaskRunStatus.Skipped).ToArray();
        if (contributing.Length == 0)
        {
            ProgressPct = 0m;
            return;
        }

        decimal totalWeight = 0m;
        decimal weighted = 0m;

        foreach (var run in contributing)
        {
            var weight = run.ProgressWeightOrDuration();
            totalWeight += weight;
            weighted += weight * run.ProgressPct;
        }

        ProgressPct = totalWeight <= 0m
            ? Math.Round(contributing.Average(run => run.ProgressPct), 2)
            : Math.Round(weighted / totalWeight, 2);
    }

    private Result EnsureActive()
        => IsActive ? Result.Success() : Result.Failure(ExecutionErrors.ExecutionNotActiveConflict(Status));

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
