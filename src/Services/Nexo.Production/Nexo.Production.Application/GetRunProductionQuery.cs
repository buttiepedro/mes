using Nexo.BuildingBlocks.Application;

namespace Nexo.Production.Application;

/// <summary>Returns the consolidated production totals for a run.</summary>
public sealed record GetRunProductionQuery(Guid RunId) : IQuery<RunProductionDto>;
