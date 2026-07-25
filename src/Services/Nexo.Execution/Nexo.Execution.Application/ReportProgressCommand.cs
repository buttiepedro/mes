using Nexo.BuildingBlocks.Application;

namespace Nexo.Execution.Application;

/// <summary>
/// Declares partial progress on a task run (<c>POST /tasks/{id}:progress</c>). The method always travels
/// with the value; progress is never negative nor above 100.
/// </summary>
public sealed record ReportProgressCommand(
    Guid TaskRunId,
    string Method,
    decimal ProgressPct,
    decimal? Quantity = null,
    decimal? TargetQuantity = null) : ICommand;
