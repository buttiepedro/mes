using Nexo.BuildingBlocks.Application;

namespace Nexo.Execution.Application;

/// <summary>
/// Completes a task run (<c>POST /tasks/{id}:complete</c>): requires the completion criterion, the
/// mandatory evidence (E11) and the finish→finish predecessors closed. A forced close overrides them and
/// requires the admin permission (E19, checked at the API).
/// </summary>
public sealed record CompleteTaskCommand(
    Guid TaskRunId,
    bool Force = false,
    string? Reason = null) : ICommand;
