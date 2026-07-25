using Nexo.BuildingBlocks.Domain;

namespace Nexo.Execution.Domain;

// Domain events of the Execution aggregate. Each is translated by the Application layer to a canonical
// integration event (docs/design/02-event-model.md §6.1); the Infrastructure outbox performs the
// conversion when SaveChanges flushes the aggregate.

/// <summary>The run was created and its task runs materialized. Canonical: <c>nexo.execution.created</c>.</summary>
public sealed record ExecutionCreatedDomainEvent(
    Guid ExecutionId,
    string Code,
    ExecutionFlavor Flavor,
    Guid ProcessId,
    Guid ProcessVersionId,
    string VersionNo,
    int TaskRunCount) : DomainEvent;

/// <summary>The run started (its first task started). Canonical: <c>nexo.execution.started</c>.</summary>
public sealed record ExecutionStartedDomainEvent(
    Guid ExecutionId,
    DateTimeOffset StartedAt,
    Guid FirstTaskRunId) : DomainEvent;

/// <summary>The run was closed. Canonical: <c>nexo.execution.closed</c>.</summary>
public sealed record ExecutionClosedDomainEvent(
    Guid ExecutionId,
    ExecutionFlavor Flavor,
    CloseKind Mode,
    decimal ProgressPct,
    long WorkedTimeSeconds,
    string? Reason) : DomainEvent;

/// <summary>The run was cancelled; incurred time and consumption are preserved. Canonical: <c>nexo.execution.cancelled</c>.</summary>
public sealed record ExecutionCancelledDomainEvent(
    Guid ExecutionId,
    string Reason,
    long IncurredWorkedTimeSeconds) : DomainEvent;

/// <summary>A real input consumption was registered. Canonical: <c>nexo.execution.input_consumed</c>.</summary>
public sealed record ExecutionInputConsumedDomainEvent(
    Guid ExecutionId,
    Guid ConsumptionId,
    Guid? TaskRunId,
    Guid ItemId,
    decimal Quantity,
    Guid UomId,
    ConsumptionMethod Method) : DomainEvent;

/// <summary>A milestone was reached (project flavour). Canonical: <c>nexo.execution.milestone_reached</c>.</summary>
public sealed record ExecutionMilestoneReachedDomainEvent(
    Guid ExecutionId,
    Guid TaskRunId,
    DateTimeOffset? CommittedDate,
    DateTimeOffset ReachedAt) : DomainEvent;

/// <summary>A task run became <see cref="TaskRunStatus.Ready"/> (its predecessors are satisfied). Canonical: <c>nexo.task.enabled</c> (source <c>system</c>).</summary>
public sealed record TaskRunEnabledDomainEvent(
    Guid ExecutionId,
    Guid TaskRunId,
    Guid? TaskId,
    Guid RequiredRoleId,
    DateTimeOffset EnabledAt) : DomainEvent;

/// <summary>A task run was assigned to a person/crew. Canonical: <c>nexo.task.assigned</c>.</summary>
public sealed record TaskRunAssignedDomainEvent(
    Guid ExecutionId,
    Guid TaskRunId,
    AssignmentMode Mode,
    Guid? PersonId,
    Guid? RoleId) : DomainEvent;

/// <summary>A task run started; the real clock opens. Canonical: <c>nexo.task.started</c>.</summary>
public sealed record TaskRunStartedDomainEvent(
    Guid ExecutionId,
    Guid TaskRunId,
    Guid? TaskId,
    DateTimeOffset StartedAt,
    Guid? OperatorId) : DomainEvent;

/// <summary>Partial progress was declared. Canonical: <c>nexo.task.progress_reported</c>.</summary>
public sealed record TaskRunProgressReportedDomainEvent(
    Guid ExecutionId,
    Guid TaskRunId,
    decimal ProgressPct,
    ProgressMethod Method,
    decimal? Quantity) : DomainEvent;

/// <summary>A task run was blocked. Canonical: <c>nexo.task.blocked</c>.</summary>
public sealed record TaskRunBlockedDomainEvent(
    Guid ExecutionId,
    Guid TaskRunId,
    BlockCause Cause,
    Guid? ReasonCodeId,
    DateTimeOffset BlockedAt) : DomainEvent;

/// <summary>A task-run block was resolved. Canonical: <c>nexo.task.unblocked</c>.</summary>
public sealed record TaskRunUnblockedDomainEvent(
    Guid ExecutionId,
    Guid TaskRunId,
    long BlockedDurationSeconds,
    string? Resolution) : DomainEvent;

/// <summary>A task run was completed. Canonical: <c>nexo.task.completed</c>.</summary>
public sealed record TaskRunCompletedDomainEvent(
    Guid ExecutionId,
    Guid TaskRunId,
    Guid? TaskId,
    bool IsForced,
    bool IsMilestone,
    long WorkedTimeSeconds) : DomainEvent;

/// <summary>A task run was skipped with justification. Canonical: <c>nexo.task.skipped</c>.</summary>
public sealed record TaskRunSkippedDomainEvent(
    Guid ExecutionId,
    Guid TaskRunId,
    TaskObligation Obligation,
    string Reason,
    Guid? AuthorizedBy) : DomainEvent;

/// <summary>Evidence was attached to a task run. Canonical: <c>nexo.task.evidence_attached</c>.</summary>
public sealed record EvidenceAttachedDomainEvent(
    Guid ExecutionId,
    Guid TaskRunId,
    Guid EvidenceId,
    EvidenceKind Kind,
    EvidenceStatus Status,
    bool SatisfiesRequirement) : DomainEvent;
