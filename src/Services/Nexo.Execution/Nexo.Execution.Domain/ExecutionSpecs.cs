namespace Nexo.Execution.Domain;

/// <summary>
/// Immutable snapshot of a <b>published</b> process version, the raw material from which an
/// <see cref="Execution"/> materializes its task runs and freezes its DAG (E1/E2).
/// </summary>
/// <remarks>
/// Execution is a bounded context of its own: it never reads the <c>work</c> schema. In the real
/// integration this snapshot is fetched over gRPC from Work Model
/// (<c>ProcessCatalog.GetPublishedVersion</c>, docs/design/04-service-contracts.md §2.6/§2.7) and passed
/// into <see cref="Execution.Create"/>. That gRPC call is <b>pending</b> and out of scope for this slice:
/// here the snapshot is supplied by the caller. Everything it carries is frozen into the run, so a later
/// version of the process never mutates a live execution.
/// </remarks>
public sealed record ProcessSnapshot(
    Guid ProcessId,
    Guid ProcessVersionId,
    string VersionNo,
    ExecutionFlavor Flavor,
    IReadOnlyList<TaskSnapshot> Tasks,
    IReadOnlyList<PrecedenceSnapshot> Precedences);

/// <summary>One task definition of the frozen version. Its ids are logical references to <c>work.*</c>/<c>config.*</c> (§1.9).</summary>
public sealed record TaskSnapshot(
    Guid TaskId,
    string Code,
    string Name,
    Guid ResponsibleRoleId,
    Guid? SuggestedPersonId = null,
    decimal? StandardDurationSeconds = null,
    decimal? EstimatedDurationSeconds = null,
    decimal? ProgressWeight = null,
    TaskObligation Obligation = TaskObligation.Mandatory,
    bool IsMilestone = false,
    EvidenceKind? RequiredEvidenceKind = null,
    short MinEvidenceCount = 0);

/// <summary>One precedence of the frozen DAG, expressed with the <b>task ids</b> of the version.</summary>
public sealed record PrecedenceSnapshot(
    Guid PredecessorTaskId,
    Guid SuccessorTaskId,
    DependencyType Type = DependencyType.FS,
    int LagSeconds = 0);

/// <summary>
/// The polymorphic trigger that originated the execution (§4 of execution.md). The <see cref="Kind"/> is
/// the only thing that structurally distinguishes a batch from a project at birth; the reference may be
/// external (an ERP object) and carries no physical foreign key.
/// </summary>
public sealed record ExecutionTrigger(
    TriggerKind Kind,
    string? RefKind = null,
    Guid? RefId = null,
    string? ExternalRef = null);

/// <summary>Objective of a <see cref="ExecutionFlavor.Batch"/> run: product + target quantity (E4).</summary>
public sealed record BatchTarget(Guid ItemId, decimal Quantity, Guid UomId);

/// <summary>
/// Commitment of a <see cref="ExecutionFlavor.Project"/> run: the "order" lives here as an <b>attribute</b>
/// of the execution, never as a master-data catalogue (E5, §2.5.4). The customer is optional (an internal
/// project has none); the committed date is not (it is what schedule deviation is measured against).
/// </summary>
public sealed record ProjectCommitment(
    string Deliverable,
    DateTimeOffset CommittedDate,
    Guid? CustomerId = null,
    Guid? DeliverableItemId = null,
    string? ContractRef = null);

/// <summary>Physical scope resolved for the run (Capa 1); every field is optional (CB11/CB12).</summary>
public sealed record ExecutionScope(
    Guid? SiteId = null,
    Guid? AreaId = null,
    Guid? LineId = null,
    Guid? WorkCenterId = null);
