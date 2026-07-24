using Nexo.BuildingBlocks.Application;

namespace Nexo.MasterData.Application;

/// <summary>
/// Updates the editable attributes of an item. The code is the natural key and is not part of the
/// payload; an archived item cannot be updated.
/// </summary>
public sealed record UpdateItemCommand(
    Guid ItemId,
    string Name,
    IReadOnlyList<string> Roles,
    string Tracking = "none",
    string? Category = null,
    string? Family = null,
    decimal? IdealCycleTime = null,
    Guid? DefaultProcessId = null,
    string? QualitySpecs = null) : ICommand<ItemDto>;
