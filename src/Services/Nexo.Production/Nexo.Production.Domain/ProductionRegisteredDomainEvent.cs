using Nexo.BuildingBlocks.Domain;

namespace Nexo.Production.Domain;

/// <summary>
/// Raised when production (good + scrap) is registered against an open run.
/// Translated to the canonical integration event <c>nexo.production.registered</c> by the Application layer.
/// </summary>
public sealed record ProductionRegisteredDomainEvent(
    Guid RunId,
    Guid WorkOrderId,
    decimal GoodQty,
    decimal ScrapQty) : DomainEvent;
