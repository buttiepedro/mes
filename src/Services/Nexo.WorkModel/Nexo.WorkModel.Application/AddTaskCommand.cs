using Nexo.BuildingBlocks.Application;

namespace Nexo.WorkModel.Application;

/// <summary>
/// Adds a task to a draft version. Fails with 409 when the version is no longer a draft (W10).
/// </summary>
/// <remarks>
/// <paramref name="ResponsibleRoleId"/> is a logical reference to <c>config.roles</c> and each input
/// item/uom to <c>master.*</c>: they are stored as uuid <b>without a foreign key</b> (§1.9).
/// There is no cost of any kind on a task (MOD-17).
/// </remarks>
public sealed record AddTaskCommand(
    Guid ProcessId,
    Guid VersionId,
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
    IReadOnlyList<TaskInputRequest>? Inputs = null) : ICommand<WorkTaskDto>;

/// <summary>Standard consumption declared together with the task.</summary>
public sealed record TaskInputRequest(
    Guid ItemId,
    decimal Quantity,
    Guid UomId,
    string Basis = "per_unit",
    string Kind = "material",
    decimal? TolerancePct = null,
    bool IsBlocking = false,
    bool RequiresTraceability = false);
