using Nexo.BuildingBlocks.Application;

namespace Nexo.Execution.Application;

/// <summary>
/// Skips a task run with justification (<c>POST /tasks/{id}:skip</c>). A mandatory task needs an
/// authorization (E18). The skipped run leaves the progress denominator.
/// </summary>
public sealed record SkipTaskCommand(
    Guid TaskRunId,
    string Reason,
    Guid? AuthorizedBy = null) : ICommand;
