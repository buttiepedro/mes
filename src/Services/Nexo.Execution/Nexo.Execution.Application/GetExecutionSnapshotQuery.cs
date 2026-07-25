using Nexo.BuildingBlocks.Application;

namespace Nexo.Execution.Application;

/// <summary>
/// Returns a run with its task runs and materialized progress (<c>GET /executions/{id}</c> and the gRPC
/// <c>GetExecutionSnapshot</c>). The progress method always travels with the value.
/// </summary>
public sealed record GetExecutionSnapshotQuery(Guid ExecutionId) : IQuery<ExecutionSnapshotDto>;
