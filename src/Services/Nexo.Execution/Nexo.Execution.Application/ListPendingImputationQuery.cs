using Nexo.BuildingBlocks.Application;

namespace Nexo.Execution.Application;

/// <summary>
/// The imputation backlog (<c>GET /executions/pending-imputation</c>, interpreted here as task runs whose
/// work is not yet imputed to a person): the orphan-fact tray of E24. Nothing is ever discarded.
/// </summary>
public sealed record ListPendingImputationQuery(
    int Limit = PagingDefaults.DefaultLimit,
    int Offset = 0) : IQuery<IReadOnlyList<PendingImputationDto>>;
