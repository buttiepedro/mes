using Nexo.BuildingBlocks.Application;

namespace Nexo.Execution.Application;

/// <summary>
/// Registers a real input consumption (<c>POST /executions/{id}/inputs</c>). <b>No cost in the MVP
/// (MOD-17)</b> — quantity, unit and (optionally) batch; valuation arrives in V1. The quantity is strictly
/// positive.
/// </summary>
public sealed record ConsumeInputCommand(
    Guid ExecutionId,
    Guid ItemId,
    decimal Quantity,
    Guid UomId,
    string Method,
    Guid? TaskRunId = null,
    Guid? TaskInputId = null,
    decimal? PlannedQuantity = null,
    Guid? BatchId = null,
    Guid? SerialId = null,
    Guid? PersonId = null) : ICommand<InputConsumptionDto>;
