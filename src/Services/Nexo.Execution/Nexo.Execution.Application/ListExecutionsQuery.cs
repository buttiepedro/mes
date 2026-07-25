using Nexo.BuildingBlocks.Application;

namespace Nexo.Execution.Application;

/// <summary>
/// The run list — a single listing for the two flavours (<c>GET /executions</c>), filtered by flavour,
/// status, process and committed-date ceiling.
/// </summary>
public sealed record ListExecutionsQuery(
    string? Flavor = null,
    string? Status = null,
    Guid? ProcessId = null,
    DateTimeOffset? DueBefore = null,
    int Limit = PagingDefaults.DefaultLimit,
    int Offset = 0) : IQuery<IReadOnlyList<ExecutionDto>>;
