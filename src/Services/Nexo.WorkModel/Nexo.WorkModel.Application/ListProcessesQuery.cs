using Nexo.BuildingBlocks.Application;

namespace Nexo.WorkModel.Application;

/// <summary>
/// The process library, filtered by profile (<c>repetitive</c> / <c>project</c>), status and
/// free-text search over code and name.
/// </summary>
public sealed record ListProcessesQuery(
    string? Profile = null,
    string? Status = null,
    string? Search = null,
    int Limit = PagingDefaults.DefaultLimit,
    int Offset = 0) : IQuery<IReadOnlyList<ProcessDto>>;
