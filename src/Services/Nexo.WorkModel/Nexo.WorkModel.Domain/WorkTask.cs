using Nexo.BuildingBlocks.Domain;

namespace Nexo.WorkModel.Domain;

/// <summary>
/// Task definition inside a <see cref="ProcessVersion"/> (<c>work.tasks</c>). It is a child entity of
/// the version, never a root: it is created, edited and removed through the aggregate so W10
/// (published versions are immutable) holds in one place.
/// </summary>
/// <remarks>
/// Layer 2 declares the <b>estimated</b> and the <b>standard</b> duration; the real one belongs to
/// Layer 3/4 and does not live here. <b>A milestone is not an entity</b>: it is
/// <see cref="IsMilestone"/> on the task (work-model.md §4.5); the committed date belongs to the
/// instantiated task of an execution, because the commitment is of the concrete run.
/// <para>
/// <see cref="ResponsibleRoleId"/> points at <c>config.roles</c> and
/// <see cref="SuggestedPersonId"/> at <c>master.people</c>: <b>logical references without a physical
/// foreign key</b>, since neither schema belongs to this bounded context (§1.9).
/// </para>
/// <para><b>No cost.</b> There is no standard cost and no rate here (MOD-17): cost is deferred to V1.</para>
/// </remarks>
public sealed class WorkTask : Entity<Guid>
{
    private readonly List<TaskInput> _inputs = new();

    // EF Core materialization constructor.
    private WorkTask()
    {
        Code = string.Empty;
        Name = string.Empty;
    }

    private WorkTask(Guid id, Guid processVersionId, WorkTaskSpec spec)
        : base(id)
    {
        ProcessVersionId = processVersionId;
        Code = spec.Code.Trim();
        Name = spec.Name.Trim();
        Instructions = Normalize(spec.Instructions);
        DisplaySeq = spec.DisplaySeq;
        EstimatedDurationSeconds = spec.EstimatedDurationSeconds;
        StandardDurationSeconds = spec.StandardDurationSeconds;
        ProgressWeight = spec.ProgressWeight;
        ResponsibleRoleId = spec.ResponsibleRoleId;
        Completion = spec.Completion;
        CompletionSpec = Normalize(spec.CompletionSpec);
        Obligation = spec.Obligation;
        EvidencePolicyOverride = spec.EvidencePolicyOverride;
        RequiredEvidenceKind = spec.RequiredEvidenceKind;
        MinEvidenceCount = spec.MinEvidenceCount < 0 ? (short)0 : spec.MinEvidenceCount;
        RequiredCapability = Normalize(spec.RequiredCapability);
        RequiredAssetType = Normalize(spec.RequiredAssetType);
        IsMilestone = spec.IsMilestone;
        IsParallelizable = spec.IsParallelizable;
        IsRepeatable = spec.IsRepeatable;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;

        foreach (var input in spec.Inputs ?? Array.Empty<TaskInputSpec>())
        {
            _inputs.Add(TaskInput.Create(Id, processVersionId, input));
        }
    }

    public Guid ProcessVersionId { get; private set; }

    /// <summary>Natural key of the task within the version ('T5').</summary>
    public string Code { get; private set; }

    public string Name { get; private set; }

    /// <summary>Operative text; attachments travel through <c>platform.files</c>.</summary>
    public string? Instructions { get; private set; }

    /// <summary>Presentation order only — the real precedence is the DAG.</summary>
    public int DisplaySeq { get; private set; }

    /// <summary>Estimated (most likely) duration.</summary>
    public decimal? EstimatedDurationSeconds { get; private set; }

    /// <summary>Standard duration: the basis for efficiency, progress weight and takt.</summary>
    public decimal? StandardDurationSeconds { get; private set; }

    /// <summary>
    /// Explicit progress weight (0–100). When it is <c>null</c> the weight is derived from the
    /// standard duration at publish time (G6).
    /// </summary>
    public decimal? ProgressWeight { get; private set; }

    /// <summary>Logical reference to <c>config.roles</c> — no physical foreign key. Role first, person second (W3).</summary>
    public Guid ResponsibleRoleId { get; private set; }

    /// <summary>Logical reference to <c>master.people</c> — no physical foreign key. A nominated person is the exception.</summary>
    public Guid? SuggestedPersonId { get; private set; }

    public CompletionKind Completion { get; private set; }

    /// <summary>Parameters of the completion criterion (jsonb): target quantity, range, expression.</summary>
    public string? CompletionSpec { get; private set; }

    public TaskObligation Obligation { get; private set; }

    /// <summary>Override of the process policy; <c>null</c> inherits (task &gt; process &gt; tenant).</summary>
    public EvidencePolicy? EvidencePolicyOverride { get; private set; }

    /// <summary>
    /// Evidence required to close the task. The full <c>work.task_evidence_requirements</c> table (N
    /// requirements per task) is deferred: the MVP slice carries one kind plus a minimum count.
    /// </summary>
    public EvidenceKind? RequiredEvidenceKind { get; private set; }

    public short MinEvidenceCount { get; private set; }

    /// <summary>Capability required from the work center ('weld_mig'), never a concrete asset (G10/W9).</summary>
    public string? RequiredCapability { get; private set; }

    public string? RequiredAssetType { get; private set; }

    /// <summary>Milestone marker — an attribute of the task, not an entity of its own (§4.5).</summary>
    public bool IsMilestone { get; private set; }

    /// <summary>Admits several people/resources at the same time (CB4).</summary>
    public bool IsParallelizable { get; private set; }

    /// <summary>Is instantiated N times within the same execution (CB10).</summary>
    public bool IsRepeatable { get; private set; }

    public IReadOnlyCollection<TaskInput> Inputs => _inputs;

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public Guid? DeletedBy { get; private set; }

    public bool IsMandatory => Obligation == TaskObligation.Mandatory;

    /// <summary>Builds a task after <see cref="ValidateSpec"/> has accepted the spec.</summary>
    internal static WorkTask Create(Guid processVersionId, WorkTaskSpec spec)
        => new(UuidV7.NewGuid(), processVersionId, spec);

    /// <summary>Copy of this task (and of its inputs) for a derived draft version.</summary>
    internal WorkTask CopyTo(Guid processVersionId)
    {
        var copy = new WorkTask(
            UuidV7.NewGuid(),
            processVersionId,
            new WorkTaskSpec(
                Code,
                Name,
                ResponsibleRoleId,
                Completion,
                CompletionSpec,
                EstimatedDurationSeconds,
                StandardDurationSeconds,
                ProgressWeight,
                Obligation,
                IsMilestone,
                IsParallelizable,
                IsRepeatable,
                EvidencePolicyOverride,
                RequiredEvidenceKind,
                MinEvidenceCount,
                RequiredCapability,
                RequiredAssetType,
                Instructions,
                DisplaySeq));

        copy.SuggestedPersonId = SuggestedPersonId;

        foreach (var input in _inputs)
        {
            copy._inputs.Add(input.CopyTo(copy.Id, processVersionId));
        }

        return copy;
    }

    /// <summary>Nominates a concrete person (justified exception to "role first").</summary>
    internal void SuggestPerson(Guid? personId)
    {
        SuggestedPersonId = personId == Guid.Empty ? null : personId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Structural validation of a task spec; returns the domain error or <c>null</c>.</summary>
    internal static Error? ValidateSpec(WorkTaskSpec spec)
    {
        if (string.IsNullOrWhiteSpace(spec.Code))
        {
            return WorkModelErrors.TaskCodeRequiredInvalid;
        }

        if (string.IsNullOrWhiteSpace(spec.Name))
        {
            return WorkModelErrors.TaskNameRequiredInvalid;
        }

        var code = spec.Code.Trim();

        if (spec.ResponsibleRoleId == Guid.Empty)
        {
            return WorkModelErrors.TaskRoleRequiredInvalid(code);
        }

        // "Progress weight is never negative" is an invariant, not a validation warning.
        if (spec.ProgressWeight is < 0m or > 100m)
        {
            return WorkModelErrors.TaskWeightInvalid(code);
        }

        if (spec.StandardDurationSeconds is <= 0m || spec.EstimatedDurationSeconds is <= 0m)
        {
            return WorkModelErrors.TaskDurationInvalid(code);
        }

        var inputs = spec.Inputs ?? Array.Empty<TaskInputSpec>();

        foreach (var input in inputs)
        {
            var failure = TaskInput.Validate(input);
            if (failure is not null)
            {
                return WorkModelErrors.TaskInputInvalid(code, failure);
            }
        }

        if (inputs.Select(input => input.ItemId).Distinct().Count() != inputs.Count)
        {
            return WorkModelErrors.TaskInputInvalid(code, "the same item cannot be declared twice as an input.");
        }

        return null;
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
