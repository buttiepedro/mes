using Nexo.BuildingBlocks.Application;

namespace Nexo.Execution.Application;

/// <summary>
/// Creates a run (Released) from a <b>snapshot of the published process version</b> plus its trigger and
/// its flavour-specific objective/commitment, exactly as <c>POST /executions</c> promises (§2.7).
/// </summary>
/// <remarks>
/// The flavour derives from the process profile, not from the trigger (E3). In the real integration the
/// <see cref="Snapshot"/> is fetched over gRPC from Work Model
/// (<c>ProcessCatalog.GetPublishedVersion</c>, docs/design/04-service-contracts.md §2.6/§2.7); that call is
/// <b>pending</b>, so here the caller supplies it. Item/uom/customer/role ids are logical references to
/// <c>master.*</c>/<c>config.*</c> and are not resolved against those catalogues (§1.9).
/// </remarks>
public sealed record CreateExecutionCommand(
    string Code,
    ProcessSnapshotRequest Snapshot,
    ExecutionTriggerRequest Trigger,
    BatchTargetRequest? Target = null,
    ProjectCommitmentRequest? Commitment = null,
    Guid? OwnerPersonId = null,
    int Priority = 0,
    Guid? SiteId = null,
    Guid? AreaId = null,
    Guid? LineId = null,
    Guid? WorkCenterId = null) : ICommand<ExecutionCreatedDto>;

/// <summary>Snapshot of the published version to instantiate (frozen into the run, E1/E2).</summary>
public sealed record ProcessSnapshotRequest(
    Guid ProcessId,
    Guid ProcessVersionId,
    string VersionNo,
    string Profile,
    IReadOnlyList<TaskSnapshotRequest> Tasks,
    IReadOnlyList<PrecedenceRequest> Precedences);

/// <summary>One task definition of the frozen version.</summary>
public sealed record TaskSnapshotRequest(
    Guid TaskId,
    string Code,
    string Name,
    Guid ResponsibleRoleId,
    Guid? SuggestedPersonId = null,
    decimal? StandardDurationSeconds = null,
    decimal? EstimatedDurationSeconds = null,
    decimal? ProgressWeight = null,
    string Obligation = "mandatory",
    bool IsMilestone = false,
    string? RequiredEvidenceKind = null,
    short MinEvidenceCount = 0);

/// <summary>One precedence of the frozen DAG (task ids of the version).</summary>
public sealed record PrecedenceRequest(
    Guid PredecessorTaskId,
    Guid SuccessorTaskId,
    string Type = "FS",
    int LagSeconds = 0);

/// <summary>Polymorphic trigger that originated the run.</summary>
public sealed record ExecutionTriggerRequest(
    string Type,
    string? RefKind = null,
    Guid? RefId = null,
    string? ExternalRef = null);

/// <summary>Objective of a batch run (E4).</summary>
public sealed record BatchTargetRequest(Guid ItemId, decimal Quantity, Guid UomId);

/// <summary>Commitment of a project run (E5) — an attribute of the run, never a master-data catalogue.</summary>
public sealed record ProjectCommitmentRequest(
    string Deliverable,
    DateTimeOffset CommittedDate,
    Guid? CustomerId = null,
    Guid? DeliverableItemId = null,
    string? ContractRef = null);
