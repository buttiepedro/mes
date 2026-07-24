namespace Nexo.WorkModel.Domain;

/// <summary>
/// Everything needed to add a <see cref="WorkTask"/> to a draft version. It is an input shape, not
/// an entity: the aggregate validates it and builds the task itself.
/// </summary>
public sealed record WorkTaskSpec(
    string Code,
    string Name,
    Guid ResponsibleRoleId,
    CompletionKind Completion = CompletionKind.Declarative,
    string? CompletionSpec = null,
    decimal? EstimatedDurationSeconds = null,
    decimal? StandardDurationSeconds = null,
    decimal? ProgressWeight = null,
    TaskObligation Obligation = TaskObligation.Mandatory,
    bool IsMilestone = false,
    bool IsParallelizable = false,
    bool IsRepeatable = false,
    EvidencePolicy? EvidencePolicyOverride = null,
    EvidenceKind? RequiredEvidenceKind = null,
    short MinEvidenceCount = 0,
    string? RequiredCapability = null,
    string? RequiredAssetType = null,
    string? Instructions = null,
    int DisplaySeq = 0,
    IReadOnlyList<TaskInputSpec>? Inputs = null);

/// <summary>Standard (theoretical) consumption declared by a task.</summary>
public sealed record TaskInputSpec(
    Guid ItemId,
    decimal Quantity,
    Guid UomId,
    InputBasis Basis = InputBasis.PerUnit,
    InputKind Kind = InputKind.Material,
    decimal? TolerancePct = null,
    bool IsBlocking = false,
    bool RequiresTraceability = false);

/// <summary>
/// One precedence of the DAG, expressed with the task <b>codes</b> — the stable identifiers the
/// editor and the REST contract speak (§2.6.3).
/// </summary>
public sealed record TaskEdgeSpec(
    string FromTaskCode,
    string ToTaskCode,
    DependencyType Type = DependencyType.FS,
    int LagSeconds = 0);

/// <summary>
/// A finding of <see cref="ProcessVersion.Validate"/>. <see cref="ValidationSeverity.Blocking"/>
/// findings prevent publication; warnings are surfaced to the editor and let it through.
/// </summary>
public sealed record ProcessVersionValidationIssue(
    string Rule,
    ValidationSeverity Severity,
    string Detail)
{
    public static ProcessVersionValidationIssue Blocking(string rule, string detail)
        => new(rule, ValidationSeverity.Blocking, detail);

    public static ProcessVersionValidationIssue Warning(string rule, string detail)
        => new(rule, ValidationSeverity.Warning, detail);
}
