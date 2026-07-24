using Nexo.BuildingBlocks.Application;

namespace Nexo.WorkModel.Application;

/// <summary>
/// Runs the integral validation of a version <b>without publishing it</b> (the editor's live check,
/// <c>POST /versions/{id}:validate</c>). Returns <c>{ ok, blocking[], warnings[] }</c>: cycles,
/// orphan tasks, missing start/terminal nodes and progress-weight problems.
/// </summary>
/// <remarks>
/// It is a command and not a query because the contract exposes it as a POST action on the version;
/// it reads only and mutates nothing, so the read scope is enough to invoke it.
/// </remarks>
public sealed record ValidateVersionCommand(Guid ProcessId, Guid VersionId) : ICommand<VersionValidationDto>;
