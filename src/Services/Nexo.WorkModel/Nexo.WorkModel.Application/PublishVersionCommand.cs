using Nexo.BuildingBlocks.Application;

namespace Nexo.WorkModel.Application;

/// <summary>
/// Publishes a version: it becomes immutable and executable, and it is the <b>only</b> published
/// version of its process (CB15). Emits <c>nexo.process.version_published</c> through the outbox.
/// </summary>
/// <remarks>
/// It carries its own scope (<c>nexo.workmodel.publish</c>) because publishing is a segregation-of-duties
/// action: whoever writes a draft is not necessarily allowed to make it executable.
/// </remarks>
public sealed record PublishVersionCommand(Guid ProcessId, Guid VersionId) : ICommand<ProcessVersionDto>;
