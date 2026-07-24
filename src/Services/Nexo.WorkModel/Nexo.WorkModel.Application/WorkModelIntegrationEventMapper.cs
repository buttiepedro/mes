using Nexo.BuildingBlocks.Domain;
using Nexo.BuildingBlocks.Messaging;
using Nexo.WorkModel.Domain;

namespace Nexo.WorkModel.Application;

/// <summary>
/// Maps Work Model domain events to their public integration-event contracts (with canonical
/// <see cref="EventTypes"/> values). Used by the Infrastructure outbox conversion in SaveChanges.
/// </summary>
public static class WorkModelIntegrationEventMapper
{
    /// <summary>Returns the integration event for a domain event, or <c>null</c> if it is purely internal.</summary>
    public static IIntegrationEvent? Map(IDomainEvent domainEvent) => domainEvent switch
    {
        ProcessVersionPublishedDomainEvent e => new ProcessVersionPublishedIntegrationEvent
        {
            ProcessId = e.ProcessId,
            VersionId = e.VersionId,
            VersionNo = e.VersionNo,
            Profile = e.Profile.ToWireValue(),
            TaskCount = e.TaskCount,
            WorkloadSeconds = e.WorkloadSeconds,
            OccurredOn = e.OccurredOn
        },
        ProcessVersionSuspendedDomainEvent e => new ProcessVersionSuspendedIntegrationEvent
        {
            ProcessId = e.ProcessId,
            VersionId = e.VersionId,
            VersionNo = e.VersionNo,
            Reason = e.Reason,
            OccurredOn = e.OccurredOn
        },
        _ => null
    };
}
