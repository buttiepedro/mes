using Nexo.WorkModel.Domain;

namespace Nexo.WorkModel.Application;

/// <summary>Read model for a process of the library.</summary>
public sealed record ProcessDto(
    Guid Id,
    string Code,
    string Name,
    string Profile,
    Guid? CurrentVersionId,
    Guid? OutputItemId,
    Guid? OutputUomId,
    Guid? SiteId,
    Guid? AreaId,
    Guid? LineId,
    string EvidencePolicy,
    string SkipPolicy,
    IReadOnlyCollection<string> Tags,
    string Status,
    string? ExternalRef);

/// <summary>Read model for a version header (no graph).</summary>
public sealed record ProcessVersionDto(
    Guid Id,
    Guid ProcessId,
    string VersionNo,
    string State,
    string Profile,
    string? ChangeReason,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? SuspendedAt,
    decimal? WorkloadSeconds,
    int TaskCount);

/// <summary>Read model for a task definition.</summary>
public sealed record WorkTaskDto(
    Guid Id,
    Guid ProcessVersionId,
    string Code,
    string Name,
    string? Instructions,
    int DisplaySeq,
    Guid ResponsibleRoleId,
    Guid? SuggestedPersonId,
    decimal? EstimatedDurationSeconds,
    decimal? StandardDurationSeconds,
    decimal? ProgressWeight,
    string Completion,
    string? CompletionSpec,
    string Obligation,
    string? EvidencePolicy,
    string? RequiredEvidenceKind,
    short MinEvidenceCount,
    string? RequiredCapability,
    string? RequiredAssetType,
    bool IsMilestone,
    bool IsParallelizable,
    bool IsRepeatable,
    IReadOnlyCollection<TaskInputDto> Inputs);

/// <summary>Read model for a standard consumption declared by a task.</summary>
public sealed record TaskInputDto(
    Guid Id,
    Guid ItemId,
    decimal Quantity,
    Guid UomId,
    string Basis,
    string Kind,
    decimal? TolerancePct,
    bool IsBlocking,
    bool RequiresTraceability);

/// <summary>Read model for one precedence of the DAG.</summary>
public sealed record TaskDependencyDto(
    Guid Id,
    Guid PredecessorTaskId,
    string PredecessorCode,
    Guid SuccessorTaskId,
    string SuccessorCode,
    string Type,
    int LagSeconds);

/// <summary>Read model for a version together with its complete graph.</summary>
public sealed record ProcessVersionGraphDto(
    ProcessVersionDto Version,
    IReadOnlyCollection<WorkTaskDto> Tasks,
    IReadOnlyCollection<TaskDependencyDto> Edges);

/// <summary>One finding of the integral validation.</summary>
public sealed record ValidationIssueDto(string Rule, string Severity, string Detail);

/// <summary>Result of <c>POST /versions/{id}:validate</c> — <c>{ ok, blocking[], warnings[] }</c>.</summary>
public sealed record VersionValidationDto(
    bool Ok,
    IReadOnlyCollection<ValidationIssueDto> Blocking,
    IReadOnlyCollection<ValidationIssueDto> Warnings);

/// <summary>Result of creating a process: it is born with version 1.0 in draft.</summary>
public sealed record ProcessCreatedDto(Guid ProcessId, Guid VersionId, string VersionNo);

/// <summary>Projections from the aggregates to their read models.</summary>
public static class WorkModelProjections
{
    public static ProcessDto ToDto(this Process process) => new(
        process.Id,
        process.Code,
        process.Name,
        process.Profile.ToWireValue(),
        process.CurrentVersionId,
        process.OutputItemId,
        process.OutputUomId,
        process.SiteId,
        process.AreaId,
        process.LineId,
        process.EvidencePolicy.ToWireValue(),
        process.SkipPolicy.ToWireValue(),
        process.Tags.ToArray(),
        process.Status.ToWireValue(),
        process.ExternalRef);

    public static ProcessVersionDto ToDto(this ProcessVersion version) => new(
        version.Id,
        version.ProcessId,
        version.VersionNo,
        version.State.ToWireValue(),
        version.Profile.ToWireValue(),
        version.ChangeReason,
        version.PublishedAt,
        version.SuspendedAt,
        version.WorkloadSeconds,
        version.Tasks.Count);

    public static WorkTaskDto ToDto(this WorkTask task) => new(
        task.Id,
        task.ProcessVersionId,
        task.Code,
        task.Name,
        task.Instructions,
        task.DisplaySeq,
        task.ResponsibleRoleId,
        task.SuggestedPersonId,
        task.EstimatedDurationSeconds,
        task.StandardDurationSeconds,
        task.ProgressWeight,
        task.Completion.ToWireValue(),
        task.CompletionSpec,
        task.Obligation.ToWireValue(),
        task.EvidencePolicyOverride?.ToWireValue(),
        task.RequiredEvidenceKind?.ToWireValue(),
        task.MinEvidenceCount,
        task.RequiredCapability,
        task.RequiredAssetType,
        task.IsMilestone,
        task.IsParallelizable,
        task.IsRepeatable,
        task.Inputs.Select(input => input.ToDto()).ToArray());

    public static TaskInputDto ToDto(this TaskInput input) => new(
        input.Id,
        input.ItemId,
        input.Quantity,
        input.UomId,
        input.Basis.ToWireValue(),
        input.Kind.ToWireValue(),
        input.TolerancePct,
        input.IsBlocking,
        input.RequiresTraceability);

    public static ValidationIssueDto ToDto(this ProcessVersionValidationIssue issue) => new(
        issue.Rule,
        issue.Severity.ToWireValue(),
        issue.Detail);

    /// <summary>Projects a version and its graph; the edges carry the task codes the editor speaks.</summary>
    public static ProcessVersionGraphDto ToGraphDto(this ProcessVersion version)
    {
        var codeById = version.Tasks.ToDictionary(task => task.Id, task => task.Code);

        var edges = version.Dependencies
            .Select(dependency => new TaskDependencyDto(
                dependency.Id,
                dependency.PredecessorTaskId,
                codeById.TryGetValue(dependency.PredecessorTaskId, out var from) ? from : string.Empty,
                dependency.SuccessorTaskId,
                codeById.TryGetValue(dependency.SuccessorTaskId, out var to) ? to : string.Empty,
                dependency.Type.ToWireValue(),
                dependency.LagSeconds))
            .ToArray();

        return new ProcessVersionGraphDto(
            version.ToDto(),
            version.Tasks.OrderBy(task => task.DisplaySeq).ThenBy(task => task.Code).Select(task => task.ToDto()).ToArray(),
            edges);
    }

    /// <summary>Splits the findings into the <c>{ ok, blocking[], warnings[] }</c> shape of the contract.</summary>
    public static VersionValidationDto ToValidationDto(this IReadOnlyList<ProcessVersionValidationIssue> issues)
    {
        var blocking = issues.Where(issue => issue.Severity == ValidationSeverity.Blocking).Select(ToDto).ToArray();
        var warnings = issues.Where(issue => issue.Severity == ValidationSeverity.Warning).Select(ToDto).ToArray();

        return new VersionValidationDto(blocking.Length == 0, blocking, warnings);
    }
}
