using Nexo.BuildingBlocks.Application;

namespace Nexo.MasterData.Application;

/// <summary>Returns a single unit of measure by id.</summary>
public sealed record GetUomQuery(Guid UomId) : IQuery<UomDto>;
