using Nexo.BuildingBlocks.Application;

namespace Nexo.MasterData.Application;

/// <summary>
/// Lists items filtered by role (<c>product</c> / <c>input</c>), status and free-text search over
/// code and name.
/// </summary>
public sealed record ListItemsQuery(
    string? Role = null,
    string? Status = null,
    string? Search = null,
    int Limit = PagingDefaults.DefaultLimit,
    int Offset = 0) : IQuery<IReadOnlyList<ItemDto>>;
