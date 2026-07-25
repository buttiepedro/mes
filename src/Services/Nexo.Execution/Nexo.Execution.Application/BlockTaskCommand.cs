using Nexo.BuildingBlocks.Application;

namespace Nexo.Execution.Application;

/// <summary>
/// Declares a task-run block with its cause (<c>POST /tasks/{id}:block</c>) — the direct input of the
/// bottleneck KPI. Emits <c>nexo.task.blocked</c>.
/// </summary>
public sealed record BlockTaskCommand(
    Guid TaskRunId,
    string Cause,
    Guid? ReasonCodeId = null) : ICommand;
