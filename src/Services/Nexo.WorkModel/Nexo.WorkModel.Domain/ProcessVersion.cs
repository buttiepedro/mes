using Nexo.BuildingBlocks.Domain;

namespace Nexo.WorkModel.Domain;

/// <summary>
/// A version of a <see cref="Process"/> (<c>work.process_versions</c>) and the <b>root of the
/// graph</b>: it owns its tasks and its precedences, so the DAG invariants are enforced in one place.
/// </summary>
/// <remarks>
/// <b>Published is immutable (W10).</b> Once <see cref="Publish"/> succeeds no task, input or
/// precedence can change: a new draft is derived from it instead. Every structural operation returns
/// a <see cref="Result"/> with a domain error rather than throwing, because the editor needs to show
/// the offending rule (the cycle, the orphan task) and not a stack trace.
/// <para>
/// <b>The profile is frozen.</b> It is copied from the process when the version is created and never
/// changes: switching flavour demands a major version (W11).
/// </para>
/// </remarks>
public sealed class ProcessVersion : AggregateRoot<Guid>
{
    private readonly List<WorkTask> _tasks = new();
    private readonly List<TaskDependency> _dependencies = new();

    // EF Core materialization constructor.
    private ProcessVersion() => VersionNo = string.Empty;

    private ProcessVersion(
        Guid id,
        Guid processId,
        ProcessProfile profile,
        short major,
        short minor,
        short patch,
        string? changeReason)
        : base(id)
    {
        ProcessId = processId;
        Profile = profile;
        VersionMajor = major;
        VersionMinor = minor;
        VersionPatch = patch;
        VersionNo = FormatVersionNo(major, minor, patch);
        State = ProcessVersionState.Draft;
        ChangeReason = string.IsNullOrWhiteSpace(changeReason) ? null : changeReason.Trim();
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid ProcessId { get; private set; }

    /// <summary>'1.0', '1.3', '2.0' — major.minor[.patch] (§9.4).</summary>
    public string VersionNo { get; private set; }

    public short VersionMajor { get; private set; }

    public short VersionMinor { get; private set; }

    public short VersionPatch { get; private set; }

    public ProcessVersionState State { get; private set; }

    /// <summary>Frozen copy of the process profile: changing it demands a major version (W11).</summary>
    public ProcessProfile Profile { get; private set; }

    public string? ChangeReason { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public DateTimeOffset? SuspendedAt { get; private set; }

    /// <summary>
    /// DERIVED at publish time: sum of the standard durations — the <b>workload</b> (man-hours).
    /// It is <b>not</b> the duration of the version: parallel branches overlap, so the elapsed time is
    /// the critical path of the DAG, computed by a separate slice (out of scope here).
    /// </summary>
    public decimal? WorkloadSeconds { get; private set; }

    public IReadOnlyCollection<WorkTask> Tasks => _tasks;

    public IReadOnlyCollection<TaskDependency> Dependencies => _dependencies;

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public Guid? DeletedBy { get; private set; }

    /// <summary>Only a draft admits structural changes (W10).</summary>
    public bool IsEditable => State == ProcessVersionState.Draft;

    public bool IsPublished => State == ProcessVersionState.Published;

    /// <summary>First version of a brand-new process: 1.0 in draft.</summary>
    public static ProcessVersion CreateInitialDraft(Guid processId, ProcessProfile profile, string? changeReason = null)
        => new(UuidV7.NewGuid(), processId, profile, 1, 0, 0, changeReason);

    /// <summary>
    /// Derives a new draft from <paramref name="source"/>, copying its tasks, inputs and precedences
    /// with fresh identities. This — not editing — is how a published version evolves (W10).
    /// </summary>
    public static ProcessVersion DeriveDraft(ProcessVersion source, VersionBump bump, string? changeReason = null)
    {
        var (major, minor, patch) = bump switch
        {
            VersionBump.Major => ((short)(source.VersionMajor + 1), (short)0, (short)0),
            VersionBump.Minor => (source.VersionMajor, (short)(source.VersionMinor + 1), (short)0),
            _ => (source.VersionMajor, source.VersionMinor, (short)(source.VersionPatch + 1))
        };

        var draft = new ProcessVersion(UuidV7.NewGuid(), source.ProcessId, source.Profile, major, minor, patch, changeReason);

        var taskIdByCode = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        foreach (var task in source._tasks)
        {
            var copy = task.CopyTo(draft.Id);
            draft._tasks.Add(copy);
            taskIdByCode[copy.Code] = copy.Id;
        }

        var codeById = source._tasks.ToDictionary(task => task.Id, task => task.Code);

        foreach (var dependency in source._dependencies)
        {
            if (!codeById.TryGetValue(dependency.PredecessorTaskId, out var fromCode)
                || !codeById.TryGetValue(dependency.SuccessorTaskId, out var toCode))
            {
                continue;
            }

            draft._dependencies.Add(TaskDependency.Create(
                draft.Id,
                taskIdByCode[fromCode],
                taskIdByCode[toCode],
                dependency.Type,
                dependency.LagSeconds));
        }

        return draft;
    }

    public WorkTask? FindTask(Guid taskId) => _tasks.FirstOrDefault(task => task.Id == taskId);

    public WorkTask? FindTaskByCode(string code)
        => _tasks.FirstOrDefault(task => string.Equals(task.Code, code?.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>Adds a task to the draft. Fails when the version is not editable or the spec is invalid.</summary>
    public Result<WorkTask> AddTask(WorkTaskSpec spec)
    {
        if (!IsEditable)
        {
            return Result<WorkTask>.Failure(WorkModelErrors.VersionNotEditableConflict(VersionNo, State));
        }

        var invalid = WorkTask.ValidateSpec(spec);
        if (invalid is not null)
        {
            return Result<WorkTask>.Failure(invalid);
        }

        var code = spec.Code.Trim();

        if (FindTaskByCode(code) is not null)
        {
            return Result<WorkTask>.Failure(WorkModelErrors.TaskCodeConflict(code));
        }

        var task = WorkTask.Create(Id, spec);
        _tasks.Add(task);
        Touch();

        return Result<WorkTask>.Success(task);
    }

    /// <summary>Removes a task from the draft together with every precedence that touches it.</summary>
    public Result RemoveTask(Guid taskId)
    {
        if (!IsEditable)
        {
            return Result.Failure(WorkModelErrors.VersionNotEditableConflict(VersionNo, State));
        }

        var task = FindTask(taskId);
        if (task is null)
        {
            return Result.Failure(WorkModelErrors.TaskNotFound(taskId.ToString()));
        }

        _dependencies.RemoveAll(dependency =>
            dependency.PredecessorTaskId == taskId || dependency.SuccessorTaskId == taskId);
        _tasks.Remove(task);
        Touch();

        return Result.Success();
    }

    /// <summary>
    /// Replaces the whole precedence set of the version. Rejects unknown task codes, trivial edges
    /// (<c>A → A</c>), duplicated edges, negative lag and — the point of the whole exercise — any
    /// <b>cycle</b> of any length, reporting the cycle it found (G1).
    /// </summary>
    public Result SetGraph(IReadOnlyList<TaskEdgeSpec> edges)
    {
        if (!IsEditable)
        {
            return Result.Failure(WorkModelErrors.VersionNotEditableConflict(VersionNo, State));
        }

        var requested = edges ?? Array.Empty<TaskEdgeSpec>();
        var resolved = new List<(WorkTask From, WorkTask To, DependencyType Type, int Lag)>(requested.Count);
        var seen = new HashSet<(Guid, Guid)>();

        foreach (var edge in requested)
        {
            var from = FindTaskByCode(edge.FromTaskCode);
            if (from is null)
            {
                return Result.Failure(WorkModelErrors.TaskNotFound(edge.FromTaskCode));
            }

            var to = FindTaskByCode(edge.ToTaskCode);
            if (to is null)
            {
                return Result.Failure(WorkModelErrors.TaskNotFound(edge.ToTaskCode));
            }

            // B1: trivial cycle of length 1. A task never precedes itself.
            if (from.Id == to.Id)
            {
                return Result.Failure(WorkModelErrors.SelfDependencyInvalid(from.Code));
            }

            if (edge.LagSeconds < 0)
            {
                return Result.Failure(WorkModelErrors.NegativeLagInvalid(from.Code, to.Code));
            }

            if (!seen.Add((from.Id, to.Id)))
            {
                return Result.Failure(WorkModelErrors.DuplicateEdgeInvalid(from.Code, to.Code));
            }

            resolved.Add((from, to, edge.Type, edge.LagSeconds));
        }

        // B2: cycle of any length. Evaluated over the whole proposed graph, so reordering the DAG in
        // one shot never raises a false positive on an intermediate state.
        var cycle = TaskGraph.FindCycle(
            _tasks.Select(task => task.Id),
            resolved.Select(edge => (edge.From.Id, edge.To.Id)));

        if (cycle is not null)
        {
            return Result.Failure(WorkModelErrors.CycleInvalid(DescribeCycle(cycle)));
        }

        _dependencies.Clear();

        foreach (var edge in resolved)
        {
            _dependencies.Add(TaskDependency.Create(Id, edge.From.Id, edge.To.Id, edge.Type, edge.Lag));
        }

        Touch();

        return Result.Success();
    }

    /// <summary>
    /// Runs the integral validation of the version (barrier B3 of §2.6.3) without publishing it:
    /// G1 acyclic, G2 reachability, G3 start/terminal nodes, G6 progress weights and G7 role +
    /// completion criterion on every mandatory task.
    /// </summary>
    public IReadOnlyList<ProcessVersionValidationIssue> Validate()
    {
        var issues = new List<ProcessVersionValidationIssue>();

        if (_tasks.Count == 0)
        {
            issues.Add(ProcessVersionValidationIssue.Blocking(
                "W1",
                "The version has no tasks: there is nothing to execute."));

            return issues;
        }

        var nodes = _tasks.Select(task => task.Id).ToArray();
        var edges = _dependencies.Select(dependency => (dependency.PredecessorTaskId, dependency.SuccessorTaskId)).ToArray();

        var cycle = TaskGraph.FindCycle(nodes, edges);
        if (cycle is not null)
        {
            issues.Add(ProcessVersionValidationIssue.Blocking(
                "G1",
                $"The graph has a cycle: {string.Join(" -> ", DescribeCycle(cycle))}."));

            // Reachability over a cyclic graph is meaningless; G1 is fixed first.
            return issues;
        }

        var unreachable = TaskGraph.FindUnreachableNodes(nodes, edges);
        if (unreachable.Count > 0)
        {
            issues.Add(ProcessVersionValidationIssue.Blocking(
                "G2",
                $"Tasks unreachable from any starting node: {string.Join(", ", unreachable.Select(DescribeTask))}."));
        }

        if (TaskGraph.FindStartNodes(nodes, edges).Count == 0)
        {
            issues.Add(ProcessVersionValidationIssue.Blocking("G3", "The graph has no starting node."));
        }

        if (TaskGraph.FindTerminalNodes(nodes, edges).Count == 0)
        {
            issues.Add(ProcessVersionValidationIssue.Blocking("G3", "The graph has no terminal node."));
        }

        foreach (var task in _tasks)
        {
            if (task.ResponsibleRoleId == Guid.Empty)
            {
                issues.Add(ProcessVersionValidationIssue.Blocking(
                    "G7",
                    $"Task '{task.Code}' has no responsible role (W3)."));
            }

            if (task.ProgressWeight is < 0m)
            {
                issues.Add(ProcessVersionValidationIssue.Blocking(
                    "G6",
                    $"The progress weight of task '{task.Code}' is negative."));
            }
        }

        // G6: explicit weights normalize to 100 %. When none is declared the weight is derived from
        // the standard duration, so a version without explicit weights is valid.
        var explicitWeights = _tasks.Where(task => task.ProgressWeight.HasValue).ToArray();
        if (explicitWeights.Length > 0)
        {
            var total = explicitWeights.Sum(task => task.ProgressWeight!.Value);

            if (explicitWeights.Length != _tasks.Count)
            {
                issues.Add(ProcessVersionValidationIssue.Warning(
                    "G6",
                    "Some tasks declare an explicit progress weight and others do not; the missing ones are derived from the standard duration."));
            }
            else if (total != 100m)
            {
                issues.Add(ProcessVersionValidationIssue.Warning(
                    "G6",
                    $"The explicit progress weights add up to {total} instead of 100; they will be normalized."));
            }
        }

        return issues;
    }

    /// <summary>
    /// Publishes the version: it becomes immutable and executable. Requires a draft with no blocking
    /// findings. Raises <see cref="ProcessVersionPublishedDomainEvent"/>.
    /// </summary>
    /// <remarks>
    /// "One published version per process" is <b>not</b> checked here — it is an invariant of the
    /// process, enforced by <see cref="Process.PublishVersion"/> and by the partial unique index
    /// <c>ux_process_versions_published</c>.
    /// </remarks>
    internal Result Publish()
    {
        if (State != ProcessVersionState.Draft)
        {
            return Result.Failure(WorkModelErrors.VersionNotDraftConflict(VersionNo, State));
        }

        var blocking = Validate().Where(issue => issue.Severity == ValidationSeverity.Blocking).ToArray();
        if (blocking.Length > 0)
        {
            return Result.Failure(WorkModelErrors.VersionValidationInvalid(blocking));
        }

        State = ProcessVersionState.Published;
        PublishedAt = DateTimeOffset.UtcNow;
        WorkloadSeconds = _tasks.Sum(task => task.StandardDurationSeconds ?? 0m);
        Touch();

        Raise(new ProcessVersionPublishedDomainEvent(
            ProcessId,
            Id,
            VersionNo,
            Profile,
            _tasks.Count,
            WorkloadSeconds));

        return Result.Success();
    }

    /// <summary>
    /// Suspends the published version: running executions continue, new ones cannot be created.
    /// Raises <see cref="ProcessVersionSuspendedDomainEvent"/>.
    /// </summary>
    internal Result Suspend(string? reason)
    {
        if (State != ProcessVersionState.Published)
        {
            return Result.Failure(WorkModelErrors.VersionNotPublishedConflict(VersionNo, State));
        }

        State = ProcessVersionState.Suspended;
        SuspendedAt = DateTimeOffset.UtcNow;
        Touch();

        Raise(new ProcessVersionSuspendedDomainEvent(
            ProcessId,
            Id,
            VersionNo,
            string.IsNullOrWhiteSpace(reason) ? null : reason.Trim()));

        return Result.Success();
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;

    private IReadOnlyList<string> DescribeCycle(IReadOnlyList<Guid> cycle)
        => cycle.Select(DescribeTask).ToArray();

    private string DescribeTask(Guid taskId) => FindTask(taskId)?.Code ?? taskId.ToString();

    private static string FormatVersionNo(short major, short minor, short patch)
        => patch == 0 ? $"{major}.{minor}" : $"{major}.{minor}.{patch}";
}
