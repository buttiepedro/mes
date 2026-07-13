using Nexo.BuildingBlocks.Messaging;

namespace Nexo.Production.Application;

/// <summary>
/// Public contract published to the backbone when a run is closed (triggers the aggregated Odoo push).
/// Canonical type: <c>nexo.production.run_closed</c>.
/// </summary>
public sealed record RunClosedIntegrationEvent : IntegrationEvent
{
    public override string Type => EventTypes.Production_RunClosed;

    public Guid RunId { get; init; }

    public Guid WorkOrderId { get; init; }
}
