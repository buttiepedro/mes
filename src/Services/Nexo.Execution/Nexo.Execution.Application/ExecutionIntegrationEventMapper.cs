using Nexo.BuildingBlocks.Domain;
using Nexo.BuildingBlocks.Messaging;
using Nexo.Execution.Domain;

namespace Nexo.Execution.Application;

/// <summary>
/// Maps Execution domain events to their public integration-event contracts (with canonical
/// <see cref="EventTypes"/> values). Used by the Infrastructure outbox conversion in SaveChanges.
/// </summary>
public static class ExecutionIntegrationEventMapper
{
    /// <summary>Returns the integration event for a domain event, or <c>null</c> if it is purely internal.</summary>
    public static IIntegrationEvent? Map(IDomainEvent domainEvent) => domainEvent switch
    {
        ExecutionCreatedDomainEvent e => new ExecutionCreatedIntegrationEvent
        {
            ExecutionId = e.ExecutionId,
            Code = e.Code,
            Flavor = e.Flavor.ToWireValue(),
            ProcessId = e.ProcessId,
            ProcessVersionId = e.ProcessVersionId,
            VersionNo = e.VersionNo,
            TaskRunCount = e.TaskRunCount,
            OccurredOn = e.OccurredOn
        },
        ExecutionStartedDomainEvent e => new ExecutionStartedIntegrationEvent
        {
            ExecutionId = e.ExecutionId,
            StartedAt = e.StartedAt,
            FirstTaskRunId = e.FirstTaskRunId,
            OccurredOn = e.OccurredOn
        },
        ExecutionClosedDomainEvent e => new ExecutionClosedIntegrationEvent
        {
            ExecutionId = e.ExecutionId,
            Flavor = e.Flavor.ToWireValue(),
            Mode = e.Mode.ToWireValue(),
            ProgressPct = e.ProgressPct,
            WorkedTimeSeconds = e.WorkedTimeSeconds,
            Reason = e.Reason,
            OccurredOn = e.OccurredOn
        },
        ExecutionCancelledDomainEvent e => new ExecutionCancelledIntegrationEvent
        {
            ExecutionId = e.ExecutionId,
            Reason = e.Reason,
            IncurredWorkedTimeSeconds = e.IncurredWorkedTimeSeconds,
            OccurredOn = e.OccurredOn
        },
        ExecutionInputConsumedDomainEvent e => new ExecutionInputConsumedIntegrationEvent
        {
            ExecutionId = e.ExecutionId,
            ConsumptionId = e.ConsumptionId,
            TaskRunId = e.TaskRunId,
            ItemId = e.ItemId,
            Quantity = e.Quantity,
            UomId = e.UomId,
            Method = e.Method.ToWireValue(),
            OccurredOn = e.OccurredOn
        },
        ExecutionMilestoneReachedDomainEvent e => new ExecutionMilestoneReachedIntegrationEvent
        {
            ExecutionId = e.ExecutionId,
            TaskRunId = e.TaskRunId,
            CommittedDate = e.CommittedDate,
            ReachedAt = e.ReachedAt,
            OccurredOn = e.OccurredOn
        },
        TaskRunEnabledDomainEvent e => new TaskEnabledIntegrationEvent
        {
            ExecutionId = e.ExecutionId,
            TaskRunId = e.TaskRunId,
            TaskId = e.TaskId,
            RequiredRoleId = e.RequiredRoleId,
            EnabledAt = e.EnabledAt,
            OccurredOn = e.OccurredOn
        },
        TaskRunAssignedDomainEvent e => new TaskAssignedIntegrationEvent
        {
            ExecutionId = e.ExecutionId,
            TaskRunId = e.TaskRunId,
            Mode = e.Mode.ToWireValue(),
            PersonId = e.PersonId,
            RoleId = e.RoleId,
            OccurredOn = e.OccurredOn
        },
        TaskRunStartedDomainEvent e => new TaskStartedIntegrationEvent
        {
            ExecutionId = e.ExecutionId,
            TaskRunId = e.TaskRunId,
            TaskId = e.TaskId,
            StartedAt = e.StartedAt,
            OperatorId = e.OperatorId,
            OccurredOn = e.OccurredOn
        },
        TaskRunProgressReportedDomainEvent e => new TaskProgressReportedIntegrationEvent
        {
            ExecutionId = e.ExecutionId,
            TaskRunId = e.TaskRunId,
            ProgressPct = e.ProgressPct,
            Method = e.Method.ToWireValue(),
            Quantity = e.Quantity,
            OccurredOn = e.OccurredOn
        },
        TaskRunBlockedDomainEvent e => new TaskBlockedIntegrationEvent
        {
            ExecutionId = e.ExecutionId,
            TaskRunId = e.TaskRunId,
            Cause = e.Cause.ToWireValue(),
            ReasonCodeId = e.ReasonCodeId,
            BlockedAt = e.BlockedAt,
            OccurredOn = e.OccurredOn
        },
        TaskRunUnblockedDomainEvent e => new TaskUnblockedIntegrationEvent
        {
            ExecutionId = e.ExecutionId,
            TaskRunId = e.TaskRunId,
            BlockedDurationSeconds = e.BlockedDurationSeconds,
            Resolution = e.Resolution,
            OccurredOn = e.OccurredOn
        },
        TaskRunCompletedDomainEvent e => new TaskCompletedIntegrationEvent
        {
            ExecutionId = e.ExecutionId,
            TaskRunId = e.TaskRunId,
            TaskId = e.TaskId,
            Outcome = e.IsForced ? "forced" : "completed",
            IsMilestone = e.IsMilestone,
            WorkedTimeSeconds = e.WorkedTimeSeconds,
            OccurredOn = e.OccurredOn
        },
        TaskRunSkippedDomainEvent e => new TaskSkippedIntegrationEvent
        {
            ExecutionId = e.ExecutionId,
            TaskRunId = e.TaskRunId,
            Kind = e.Obligation.ToWireValue(),
            Reason = e.Reason,
            AuthorizedBy = e.AuthorizedBy,
            OccurredOn = e.OccurredOn
        },
        EvidenceAttachedDomainEvent e => new TaskEvidenceAttachedIntegrationEvent
        {
            ExecutionId = e.ExecutionId,
            TaskRunId = e.TaskRunId,
            EvidenceId = e.EvidenceId,
            Kind = e.Kind.ToWireValue(),
            Status = e.Status.ToWireValue(),
            SatisfiesRequirement = e.SatisfiesRequirement,
            OccurredOn = e.OccurredOn
        },
        _ => null
    };
}
