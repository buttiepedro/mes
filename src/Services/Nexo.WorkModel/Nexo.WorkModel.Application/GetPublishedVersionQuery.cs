using Nexo.BuildingBlocks.Application;

namespace Nexo.WorkModel.Application;

/// <summary>
/// Returns the published version of a process with its <b>complete graph</b> (tasks, inputs and
/// precedences). This is what Execution reads to freeze and instantiate a run.
/// </summary>
public sealed record GetPublishedVersionQuery(Guid ProcessId) : IQuery<ProcessVersionGraphDto>;
