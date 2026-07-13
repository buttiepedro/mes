using Nexo.BuildingBlocks.Domain;

namespace Nexo.Production.Domain;

/// <summary>
/// Raised when a production run is closed.
/// Translated to the canonical integration event <c>nexo.production.run_closed</c> by the Application layer.
/// </summary>
public sealed record RunClosedDomainEvent(
    Guid RunId,
    Guid WorkOrderId) : DomainEvent;
