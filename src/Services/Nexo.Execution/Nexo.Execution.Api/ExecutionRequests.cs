using Nexo.Execution.Application;

namespace Nexo.Execution.Api;

/// <summary>
/// Request body for <c>POST /v1/executions</c> — create a run from a snapshot of the published process
/// version plus its trigger and flavour-specific objective/commitment (§2.7). Reuses the Application
/// request shapes for the snapshot, trigger, target and commitment.
/// </summary>
public sealed record CreateExecutionRequest(
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
    Guid? WorkCenterId = null);

/// <summary>Request body for <c>POST /v1/tasks/{taskRunId}:take</c> — the operator's self-assignment.</summary>
public sealed record TakeTaskRequest(
    Guid? PersonId = null,
    Guid? RoleId = null,
    string Mode = "individual");

/// <summary>Request body for <c>POST /v1/tasks/{taskRunId}:start</c> — opens the real clock.</summary>
public sealed record StartTaskRequest(Guid? OperatorId = null);

/// <summary>Request body for <c>POST /v1/tasks/{taskRunId}:progress</c> — the method travels with the value.</summary>
public sealed record ReportProgressRequest(
    string Method,
    decimal ProgressPct,
    decimal? Quantity = null,
    decimal? TargetQuantity = null);

/// <summary>Request body for <c>POST /v1/tasks/{taskRunId}:block</c> — the direct input of the bottleneck KPI.</summary>
public sealed record BlockTaskRequest(string Cause, Guid? ReasonCodeId = null);

/// <summary>Request body for <c>POST /v1/tasks/{taskRunId}:unblock</c>.</summary>
public sealed record UnblockTaskRequest(string? Resolution = null);

/// <summary>Request body for <c>POST /v1/tasks/{taskRunId}:complete</c> — a forced close overrides the checklist (E19).</summary>
public sealed record CompleteTaskRequest(bool Force = false, string? Reason = null);

/// <summary>Request body for <c>POST /v1/tasks/{taskRunId}:skip</c> — a mandatory task needs an authorization (E18).</summary>
public sealed record SkipTaskRequest(string Reason, Guid? AuthorizedBy = null);

/// <summary>
/// Request body for <c>POST /v1/tasks/{taskRunId}/evidence</c> — attach (or materialize) evidence. The
/// binary is referenced, never inlined; <see cref="ContentHash"/> is a hex string.
/// </summary>
public sealed record AttachEvidenceRequest(
    string Kind,
    string Status = "pending",
    Guid? FileId = null,
    string? MediaRef = null,
    string? ContentHash = null,
    Guid? RequirementId = null,
    Guid? CapturedBy = null,
    string? Caption = null);

/// <summary>Request body for <c>POST /v1/executions/{executionId}/inputs</c> — a real consumption (no cost, MOD-17).</summary>
public sealed record ConsumeInputRequest(
    Guid ItemId,
    decimal Quantity,
    Guid UomId,
    string Method,
    Guid? TaskRunId = null,
    Guid? TaskInputId = null,
    decimal? PlannedQuantity = null,
    Guid? BatchId = null,
    Guid? SerialId = null,
    Guid? PersonId = null);

/// <summary>Request body for <c>POST /v1/executions/{executionId}:close</c>.</summary>
public sealed record CloseExecutionRequest(string Mode = "normal", string? Reason = null);

/// <summary>Request body for <c>POST /v1/executions/{executionId}:cancel</c>.</summary>
public sealed record CancelExecutionRequest(string Reason);
