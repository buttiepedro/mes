using Nexo.BuildingBlocks.Application;

namespace Nexo.WorkModel.Application;

/// <summary>
/// Removes a task from a draft version together with every precedence that touches it.
/// Fails with 409 when the version is no longer a draft (W10).
/// </summary>
public sealed record RemoveTaskCommand(Guid ProcessId, Guid VersionId, Guid TaskId) : ICommand;
