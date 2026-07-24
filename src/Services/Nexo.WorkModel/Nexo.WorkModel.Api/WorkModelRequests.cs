using Nexo.WorkModel.Application;

namespace Nexo.WorkModel.Api;

/// <summary>
/// Request body for <c>POST /v1/processes</c>. The process is born with its version 1.0 in draft.
/// The output/scope ids are logical references to <c>master.*</c> / <c>config.*</c> (§1.9).
/// </summary>
public sealed record CreateProcessRequest(
    string Code,
    string Name,
    string Profile,
    Guid? OutputItemId = null,
    Guid? OutputUomId = null,
    Guid? SiteId = null,
    Guid? AreaId = null,
    Guid? LineId = null,
    string EvidencePolicy = "recommended",
    string SkipPolicy = "authorized",
    IReadOnlyList<string>? Tags = null,
    string? ExternalRef = null);

/// <summary>Request body for <c>POST /v1/processes/{processId}/versions</c> — derive a new draft.</summary>
public sealed record CreateDraftVersionRequest(
    string Bump = "minor",
    string? ChangeReason = null);

/// <summary>
/// Request body for <c>POST /v1/processes/{processId}/versions/{versionId}/tasks</c>. Reuses the
/// Application <see cref="TaskInputRequest"/> shape for the declared inputs.
/// </summary>
public sealed record AddTaskRequest(
    string Code,
    string Name,
    Guid ResponsibleRoleId,
    string CompletionKind = "declarative",
    string? CompletionSpec = null,
    decimal? EstimatedDurationSeconds = null,
    decimal? StandardDurationSeconds = null,
    decimal? ProgressWeight = null,
    string Obligation = "mandatory",
    bool IsMilestone = false,
    bool IsParallelizable = false,
    bool IsRepeatable = false,
    string? EvidencePolicy = null,
    string? RequiredEvidenceKind = null,
    short MinEvidenceCount = 0,
    string? RequiredCapability = null,
    string? RequiredAssetType = null,
    string? Instructions = null,
    int DisplaySeq = 0,
    IReadOnlyList<TaskInputRequest>? Inputs = null);

/// <summary>
/// Request body for <c>PUT /v1/processes/{processId}/versions/{versionId}/graph</c> — replaces the whole
/// precedence set. Reuses the Application <see cref="GraphEdgeRequest"/> shape.
/// </summary>
public sealed record SetGraphRequest(IReadOnlyList<GraphEdgeRequest> Edges);

/// <summary>Request body for <c>POST /v1/processes/{processId}/versions/{versionId}:suspend</c>.</summary>
public sealed record SuspendVersionRequest(string? Reason = null);
