using Nexo.BuildingBlocks.Application;

namespace Nexo.MasterData.Application;

/// <summary>
/// Creates an item. Absolute floor: code + name + base unit. <c>product</c> and <c>input</c> are
/// roles of the same item, so <paramref name="Roles"/> may carry one or both.
/// Returns the id of the created item.
/// </summary>
public sealed record CreateItemCommand(
    string Code,
    string Name,
    string BaseUom,
    IReadOnlyList<string> Roles,
    string Tracking = "none",
    string? Category = null,
    string? Family = null,
    decimal? IdealCycleTime = null,
    Guid? DefaultProcessId = null,
    string? QualitySpecs = null,
    string? ExternalRef = null) : ICommand<Guid>;
