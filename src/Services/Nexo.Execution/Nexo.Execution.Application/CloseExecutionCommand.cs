using Nexo.BuildingBlocks.Application;

namespace Nexo.Execution.Application;

/// <summary>
/// Closes a run (<c>POST /executions/{id}:close</c>). A <c>normal</c> close is rejected while mandatory
/// task runs are still open; <c>partial</c>/<c>forced</c> override the checklist (E19). Emits
/// <c>nexo.execution.closed</c>.
/// </summary>
public sealed record CloseExecutionCommand(
    Guid ExecutionId,
    string Mode = "normal",
    string? Reason = null) : ICommand<ExecutionDto>;
