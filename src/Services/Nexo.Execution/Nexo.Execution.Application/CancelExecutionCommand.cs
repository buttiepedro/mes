using Nexo.BuildingBlocks.Application;

namespace Nexo.Execution.Application;

/// <summary>
/// Cancels a run (<c>POST /executions/{id}:cancel</c>), preserving incurred time and consumption (never a
/// destructive edit, E22). Emits <c>nexo.execution.cancelled</c>.
/// </summary>
public sealed record CancelExecutionCommand(Guid ExecutionId, string Reason) : ICommand<ExecutionDto>;
