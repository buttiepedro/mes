using Nexo.BuildingBlocks.Application;

namespace Nexo.WorkModel.Application;

/// <summary>
/// Suspends the version in force: <b>running executions continue</b> with their frozen copy, new
/// ones cannot be created. Emits <c>nexo.process.version_suspended</c> through the outbox.
/// </summary>
public sealed record SuspendVersionCommand(
    Guid ProcessId,
    Guid VersionId,
    string? Reason = null) : ICommand<ProcessVersionDto>;
