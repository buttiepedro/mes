using Nexo.BuildingBlocks.Domain;
using Nexo.BuildingBlocks.Messaging;
using Nexo.Production.Domain;

namespace Nexo.Production.Application;

/// <summary>
/// Maps Production domain events to their public integration-event contracts (with canonical
/// <see cref="EventTypes"/> values). Used by the Infrastructure outbox conversion in SaveChanges.
/// </summary>
public static class ProductionIntegrationEventMapper
{
    /// <summary>Returns the integration event for a domain event, or <c>null</c> if it is purely internal.</summary>
    public static IIntegrationEvent? Map(IDomainEvent domainEvent) => domainEvent switch
    {
        ProductionRegisteredDomainEvent e => new ProductionRegisteredIntegrationEvent
        {
            RunId = e.RunId,
            WorkOrderId = e.WorkOrderId,
            GoodQty = e.GoodQty,
            ScrapQty = e.ScrapQty,
            OccurredOn = e.OccurredOn
        },
        RunClosedDomainEvent e => new RunClosedIntegrationEvent
        {
            RunId = e.RunId,
            WorkOrderId = e.WorkOrderId,
            OccurredOn = e.OccurredOn
        },
        _ => null
    };
}
