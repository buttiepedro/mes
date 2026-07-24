using Nexo.BuildingBlocks.Application;

namespace Nexo.MasterData.Application;

/// <summary>Returns a single item by id.</summary>
public sealed record GetItemQuery(Guid ItemId) : IQuery<ItemDto>;
