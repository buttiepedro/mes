using Nexo.BuildingBlocks.Messaging;

namespace Nexo.Production.Application;

/// <summary>
/// Public contract published to the backbone when production is registered.
/// Canonical type: <c>nexo.production.registered</c>.
/// </summary>
public sealed record ProductionRegisteredIntegrationEvent : IntegrationEvent
{
    public override string Type => EventTypes.Production_Registered;

    public Guid RunId { get; init; }

    public Guid WorkOrderId { get; init; }

    public decimal GoodQty { get; init; }

    public decimal ScrapQty { get; init; }
}
