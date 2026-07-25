using Nexo.BuildingBlocks.Application;

namespace Nexo.Execution.Application;

/// <summary>
/// The operator's self-assignment from the tablet (<c>POST /tasks/{id}:take</c>). Resolves role→person
/// on the concrete run (CB19), emitting <c>nexo.task.assigned</c>.
/// </summary>
public sealed record TakeTaskCommand(
    Guid TaskRunId,
    Guid? PersonId = null,
    Guid? RoleId = null,
    string Mode = "individual") : ICommand;
