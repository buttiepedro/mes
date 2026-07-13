using Nexo.BuildingBlocks.Application;

namespace Nexo.Production.Application;

/// <summary>
/// Registers manual/datalogger production against an open run (caso estrella).
/// Returns the id of the created production record.
/// </summary>
public sealed record RegisterProductionCommand(
    Guid RunId,
    decimal GoodQty,
    decimal ScrapQty,
    Guid OperatorId,
    string Source) : ICommand<Guid>;
