using Nexo.BuildingBlocks.Application;

namespace Nexo.MasterData.Application;

/// <summary>Lists minimal customers, optionally filtered by status and free-text search.</summary>
public sealed record ListCustomersQuery(
    string? Status = null,
    string? Search = null,
    int Limit = PagingDefaults.DefaultLimit,
    int Offset = 0) : IQuery<IReadOnlyList<CustomerDto>>;
