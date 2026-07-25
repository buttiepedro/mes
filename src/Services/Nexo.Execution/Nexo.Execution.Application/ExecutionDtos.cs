using Nexo.Execution.Domain;

namespace Nexo.Execution.Application;

/// <summary>Result of creating a run: it is born released with its task runs materialized.</summary>
public sealed record ExecutionCreatedDto(
    Guid ExecutionId,
    string Code,
    string Flavor,
    string Status,
    int TaskRunCount);

/// <summary>Read model for an execution header (no graph).</summary>
public sealed record ExecutionDto(
    Guid Id,
    string Code,
    string Flavor,
    string Status,
    Guid ProcessId,
    Guid ProcessVersionId,
    string? VersionNo,
    string TriggerKind,
    string? TriggerRefKind,
    Guid? TriggerRefId,
    string? TriggerExternalRef,
    Guid? TargetItemId,
    decimal? TargetQuantity,
    Guid? TargetUomId,
    decimal GoodQuantity,
    decimal RejectQuantity,
    string? Deliverable,
    Guid? DeliverableItemId,
    Guid? CustomerId,
    DateTimeOffset? CommittedDate,
    string? ContractRef,
    Guid? OwnerPersonId,
    int Priority,
    decimal ProgressPct,
    string ProgressMethod,
    DateTimeOffset? ActualStartAt,
    DateTimeOffset? ActualEndAt,
    long WorkedTimeSeconds,
    bool SupportsOee,
    string? CloseKind,
    string? CloseReason);

/// <summary>Read model for an instantiated task (the operator's "my tasks now" row).</summary>
public sealed record TaskRunDto(
    Guid Id,
    Guid ExecutionId,
    Guid? TaskId,
    string? Code,
    string? Name,
    string Status,
    Guid? AssignedRoleId,
    Guid? AssignedPersonId,
    string AssignmentMode,
    decimal? StandardDurationSeconds,
    decimal? EstimatedDurationSeconds,
    decimal? ProgressWeight,
    long ActualTotalSeconds,
    decimal ProgressPct,
    string? ProgressMethod,
    decimal? ProducedQuantity,
    decimal? TargetQuantity,
    bool IsMilestone,
    DateTimeOffset? MilestoneCommittedDate,
    DateTimeOffset? MilestoneReachedAt,
    string Obligation,
    string? RequiredEvidenceKind,
    short MinEvidenceCount,
    DateTimeOffset? ActualStartAt,
    DateTimeOffset? ActualEndAt,
    IReadOnlyCollection<TaskRunPrecedenceDto> Precedences);

/// <summary>Read model for one incoming precedence of a task run (frozen DAG edge).</summary>
public sealed record TaskRunPrecedenceDto(Guid PredecessorTaskId, string Type, int LagSeconds);

/// <summary>Read model for a real input consumption (no cost, MOD-17).</summary>
public sealed record InputConsumptionDto(
    Guid Id,
    Guid ExecutionId,
    Guid? TaskRunId,
    Guid? TaskInputId,
    Guid ItemId,
    decimal Quantity,
    Guid UomId,
    decimal? PlannedQuantity,
    string Method,
    Guid? BatchId,
    Guid? SerialId,
    DateTimeOffset RecordedAt);

/// <summary>Read model for a piece of evidence (the binary is referenced, never inlined).</summary>
public sealed record EvidenceDto(
    Guid Id,
    Guid ExecutionId,
    Guid? TaskRunId,
    string Kind,
    string Status,
    Guid? RequirementId,
    Guid? FileId,
    string? MediaRef,
    bool IsMandatory,
    DateTimeOffset CapturedAt,
    string? Caption);

/// <summary>Read model for a run together with its task runs and materialized progress.</summary>
public sealed record ExecutionSnapshotDto(
    ExecutionDto Execution,
    IReadOnlyCollection<TaskRunDto> TaskRuns,
    decimal ProgressPct);

/// <summary>Read model for the imputation backlog (task runs whose work is not imputed yet).</summary>
public sealed record PendingImputationDto(
    Guid TaskRunId,
    Guid ExecutionId,
    string ExecutionCode,
    string Flavor,
    string? Name,
    string Status,
    Guid? AssignedRoleId,
    long WorkedTimeSeconds,
    DateTimeOffset? ActualEndAt);

/// <summary>Projections from the aggregate to its read models.</summary>
public static class ExecutionProjections
{
    public static ExecutionDto ToDto(this Domain.Execution execution) => new(
        execution.Id,
        execution.Code,
        execution.Flavor.ToWireValue(),
        execution.Status.ToWireValue(),
        execution.ProcessId,
        execution.ProcessVersionId,
        execution.VersionNo,
        execution.TriggerKind.ToWireValue(),
        execution.TriggerRefKind,
        execution.TriggerRefId,
        execution.TriggerExternalRef,
        execution.TargetItemId,
        execution.TargetQuantity,
        execution.TargetUomId,
        execution.GoodQuantity,
        execution.RejectQuantity,
        execution.Deliverable,
        execution.DeliverableItemId,
        execution.CustomerId,
        execution.CommittedDate,
        execution.ContractRef,
        execution.OwnerPersonId,
        execution.Priority,
        execution.ProgressPct,
        execution.ProgressMethod,
        execution.ActualStartAt,
        execution.ActualEndAt,
        execution.WorkedTimeSeconds,
        execution.SupportsOee,
        execution.CloseKind?.ToWireValue(),
        execution.CloseReason);

    public static TaskRunDto ToDto(this TaskRun run) => new(
        run.Id,
        run.ExecutionId,
        run.TaskId,
        run.Code,
        run.Name,
        run.Status.ToWireValue(),
        run.AssignedRoleId,
        run.AssignedPersonId,
        run.AssignmentMode.ToWireValue(),
        run.StandardDurationSeconds,
        run.EstimatedDurationSeconds,
        run.ProgressWeight,
        run.ActualTotalSeconds,
        run.ProgressPct,
        run.ProgressMethod?.ToWireValue(),
        run.ProducedQuantity,
        run.TargetQuantity,
        run.IsMilestone,
        run.MilestoneCommittedDate,
        run.MilestoneReachedAt,
        run.Obligation.ToWireValue(),
        run.RequiredEvidenceKind?.ToWireValue(),
        run.MinEvidenceCount,
        run.ActualStartAt,
        run.ActualEndAt,
        run.Precedences
            .Select(p => new TaskRunPrecedenceDto(p.PredecessorTaskId, p.Type.ToWireValue(), p.LagSeconds))
            .ToArray());

    public static InputConsumptionDto ToDto(this InputConsumption consumption) => new(
        consumption.Id,
        consumption.ExecutionId,
        consumption.TaskRunId,
        consumption.TaskInputId,
        consumption.ItemId,
        consumption.Quantity,
        consumption.UomId,
        consumption.PlannedQuantity,
        consumption.Method.ToWireValue(),
        consumption.BatchId,
        consumption.SerialId,
        consumption.RecordedAt);

    public static EvidenceDto ToDto(this Evidence evidence) => new(
        evidence.Id,
        evidence.ExecutionId,
        evidence.TaskRunId,
        evidence.Kind.ToWireValue(),
        evidence.Status.ToWireValue(),
        evidence.RequirementId,
        evidence.FileId,
        evidence.MediaRef,
        evidence.IsMandatory,
        evidence.CapturedAt,
        evidence.Caption);

    /// <summary>Projects a run and its task runs, ordered for presentation.</summary>
    public static ExecutionSnapshotDto ToSnapshotDto(this Domain.Execution execution) => new(
        execution.ToDto(),
        execution.TaskRuns
            .OrderBy(run => run.Code ?? run.Name)
            .Select(run => run.ToDto())
            .ToArray(),
        execution.ProgressPct);

    public static PendingImputationDto ToDto(this TaskRunImputationRow row) => new(
        row.TaskRunId,
        row.ExecutionId,
        row.ExecutionCode,
        row.Flavor,
        row.Name,
        row.Status,
        row.AssignedRoleId,
        row.WorkedTimeSeconds,
        row.ActualEndAt);
}
