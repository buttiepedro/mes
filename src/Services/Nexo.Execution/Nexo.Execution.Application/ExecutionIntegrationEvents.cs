using Nexo.BuildingBlocks.Messaging;

namespace Nexo.Execution.Application;

// Public integration-event contracts published to the backbone by Execution
// (docs/design/02-event-model.md §6.1). Grouped in one file for the whole slice; each carries its
// canonical EventTypes value. The Infrastructure outbox converts the domain events in SaveChanges.

/// <summary>Canonical type: <c>nexo.execution.created</c>.</summary>
public sealed record ExecutionCreatedIntegrationEvent : IntegrationEvent
{
    public override string Type => EventTypes.Execution_Created;

    public Guid ExecutionId { get; init; }

    public string Code { get; init; } = string.Empty;

    /// <summary>batch | project.</summary>
    public string Flavor { get; init; } = string.Empty;

    public Guid ProcessId { get; init; }

    public Guid ProcessVersionId { get; init; }

    public string VersionNo { get; init; } = string.Empty;

    public int TaskRunCount { get; init; }
}

/// <summary>Canonical type: <c>nexo.execution.started</c>.</summary>
public sealed record ExecutionStartedIntegrationEvent : IntegrationEvent
{
    public override string Type => EventTypes.Execution_Started;

    public Guid ExecutionId { get; init; }

    public DateTimeOffset StartedAt { get; init; }

    public Guid FirstTaskRunId { get; init; }
}

/// <summary>Canonical type: <c>nexo.execution.closed</c>.</summary>
public sealed record ExecutionClosedIntegrationEvent : IntegrationEvent
{
    public override string Type => EventTypes.Execution_Closed;

    public Guid ExecutionId { get; init; }

    public string Flavor { get; init; } = string.Empty;

    /// <summary>normal | partial | forced | cancelled | expired.</summary>
    public string Mode { get; init; } = string.Empty;

    public decimal ProgressPct { get; init; }

    public long WorkedTimeSeconds { get; init; }

    public string? Reason { get; init; }
}

/// <summary>Canonical type: <c>nexo.execution.cancelled</c>.</summary>
public sealed record ExecutionCancelledIntegrationEvent : IntegrationEvent
{
    public override string Type => EventTypes.Execution_Cancelled;

    public Guid ExecutionId { get; init; }

    public string Reason { get; init; } = string.Empty;

    public long IncurredWorkedTimeSeconds { get; init; }
}

/// <summary>Canonical type: <c>nexo.execution.input_consumed</c>. Sin costo en el MVP.</summary>
public sealed record ExecutionInputConsumedIntegrationEvent : IntegrationEvent
{
    public override string Type => EventTypes.Execution_InputConsumed;

    public Guid ExecutionId { get; init; }

    public Guid ConsumptionId { get; init; }

    public Guid? TaskRunId { get; init; }

    public Guid ItemId { get; init; }

    public decimal Quantity { get; init; }

    public Guid UomId { get; init; }

    /// <summary>declared | backflush | scale | scan | adjustment.</summary>
    public string Method { get; init; } = string.Empty;
}

/// <summary>Canonical type: <c>nexo.execution.milestone_reached</c>. Solo sabor Proyecto.</summary>
public sealed record ExecutionMilestoneReachedIntegrationEvent : IntegrationEvent
{
    public override string Type => EventTypes.Execution_MilestoneReached;

    public Guid ExecutionId { get; init; }

    public Guid TaskRunId { get; init; }

    public DateTimeOffset? CommittedDate { get; init; }

    public DateTimeOffset ReachedAt { get; init; }
}

/// <summary>Canonical type: <c>nexo.task.enabled</c> (source <c>system</c>).</summary>
public sealed record TaskEnabledIntegrationEvent : IntegrationEvent
{
    public override string Type => EventTypes.Task_Enabled;

    public Guid ExecutionId { get; init; }

    public Guid TaskRunId { get; init; }

    public Guid? TaskId { get; init; }

    public Guid RequiredRoleId { get; init; }

    public DateTimeOffset EnabledAt { get; init; }
}

/// <summary>Canonical type: <c>nexo.task.assigned</c>.</summary>
public sealed record TaskAssignedIntegrationEvent : IntegrationEvent
{
    public override string Type => EventTypes.Task_Assigned;

    public Guid ExecutionId { get; init; }

    public Guid TaskRunId { get; init; }

    /// <summary>individual | crew | role_open | automatic | external.</summary>
    public string Mode { get; init; } = string.Empty;

    public Guid? PersonId { get; init; }

    public Guid? RoleId { get; init; }
}

/// <summary>Canonical type: <c>nexo.task.started</c>.</summary>
public sealed record TaskStartedIntegrationEvent : IntegrationEvent
{
    public override string Type => EventTypes.Task_Started;

    public Guid ExecutionId { get; init; }

    public Guid TaskRunId { get; init; }

    public Guid? TaskId { get; init; }

    public DateTimeOffset StartedAt { get; init; }

    public Guid? OperatorId { get; init; }
}

/// <summary>Canonical type: <c>nexo.task.progress_reported</c>.</summary>
public sealed record TaskProgressReportedIntegrationEvent : IntegrationEvent
{
    public override string Type => EventTypes.Task_ProgressReported;

    public Guid ExecutionId { get; init; }

    public Guid TaskRunId { get; init; }

    public decimal ProgressPct { get; init; }

    /// <summary>declared | quantity | checklist | time | signal.</summary>
    public string Method { get; init; } = string.Empty;

    public decimal? Quantity { get; init; }
}

/// <summary>Canonical type: <c>nexo.task.blocked</c>.</summary>
public sealed record TaskBlockedIntegrationEvent : IntegrationEvent
{
    public override string Type => EventTypes.Task_Blocked;

    public Guid ExecutionId { get; init; }

    public Guid TaskRunId { get; init; }

    /// <summary>input | resource | approval | quality.</summary>
    public string Cause { get; init; } = string.Empty;

    public Guid? ReasonCodeId { get; init; }

    public DateTimeOffset BlockedAt { get; init; }
}

/// <summary>Canonical type: <c>nexo.task.unblocked</c>.</summary>
public sealed record TaskUnblockedIntegrationEvent : IntegrationEvent
{
    public override string Type => EventTypes.Task_Unblocked;

    public Guid ExecutionId { get; init; }

    public Guid TaskRunId { get; init; }

    public long BlockedDurationSeconds { get; init; }

    public string? Resolution { get; init; }
}

/// <summary>Canonical type: <c>nexo.task.completed</c>.</summary>
public sealed record TaskCompletedIntegrationEvent : IntegrationEvent
{
    public override string Type => EventTypes.Task_Completed;

    public Guid ExecutionId { get; init; }

    public Guid TaskRunId { get; init; }

    public Guid? TaskId { get; init; }

    /// <summary>completed | forced.</summary>
    public string Outcome { get; init; } = string.Empty;

    public bool IsMilestone { get; init; }

    public long WorkedTimeSeconds { get; init; }
}

/// <summary>Canonical type: <c>nexo.task.skipped</c>.</summary>
public sealed record TaskSkippedIntegrationEvent : IntegrationEvent
{
    public override string Type => EventTypes.Task_Skipped;

    public Guid ExecutionId { get; init; }

    public Guid TaskRunId { get; init; }

    /// <summary>mandatory | optional | conditional.</summary>
    public string Kind { get; init; } = string.Empty;

    public string Reason { get; init; } = string.Empty;

    public Guid? AuthorizedBy { get; init; }
}

/// <summary>Canonical type: <c>nexo.task.evidence_attached</c>.</summary>
public sealed record TaskEvidenceAttachedIntegrationEvent : IntegrationEvent
{
    public override string Type => EventTypes.Task_EvidenceAttached;

    public Guid ExecutionId { get; init; }

    public Guid TaskRunId { get; init; }

    public Guid EvidenceId { get; init; }

    /// <summary>photo | file | sensor_reading | signature | video | form.</summary>
    public string Kind { get; init; } = string.Empty;

    /// <summary>pending | materialized | verified.</summary>
    public string Status { get; init; } = string.Empty;

    public bool SatisfiesRequirement { get; init; }
}
