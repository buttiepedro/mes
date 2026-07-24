using Nexo.BuildingBlocks.Application;

namespace Nexo.MasterData.Application;

/// <summary>Lists units of measure, optionally filtered by magnitude and status.</summary>
public sealed record ListUomsQuery(
    string? Magnitude = null,
    string? Status = null,
    int Limit = PagingDefaults.DefaultLimit,
    int Offset = 0) : IQuery<IReadOnlyList<UomDto>>;
