using Nexo.BuildingBlocks.Application;

namespace Nexo.Execution.Application;

/// <summary>
/// Starts a task run (<c>POST /tasks/{id}:start</c>): opens the real clock and starts the execution if it
/// is the first task. Fails with E6/E7 when the predecessors are not satisfied yet.
/// </summary>
public sealed record StartTaskCommand(Guid TaskRunId, Guid? OperatorId = null) : ICommand;
