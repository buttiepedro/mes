using Nexo.BuildingBlocks.Application;

namespace Nexo.Execution.Application;

/// <summary>
/// Resolves a task-run block (<c>POST /tasks/{id}:unblock</c>), emitting <c>nexo.task.unblocked</c> with
/// the block duration.
/// </summary>
public sealed record UnblockTaskCommand(Guid TaskRunId, string? Resolution = null) : ICommand;
