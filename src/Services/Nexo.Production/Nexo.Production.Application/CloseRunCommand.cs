using Nexo.BuildingBlocks.Application;

namespace Nexo.Production.Application;

/// <summary>Closes a production run, consolidating totals and emitting <c>nexo.production.run_closed</c>.</summary>
public sealed record CloseRunCommand(Guid RunId) : ICommand;
