using Nexo.BuildingBlocks.Application;

namespace Nexo.MasterData.Application;

/// <summary>
/// Creates a unit of measure. Conversion is only ever declared <b>within</b> the same magnitude:
/// <paramref name="FactorToBase"/> expresses this unit in terms of the base unit of its magnitude.
/// Returns the id of the created unit.
/// </summary>
public sealed record CreateUomCommand(
    string Code,
    string Name,
    string Symbol,
    string Magnitude,
    decimal FactorToBase,
    bool IsBase = false,
    short Decimals = 4,
    string? ExternalRef = null) : ICommand<Guid>;
