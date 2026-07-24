using Nexo.BuildingBlocks.Application;

namespace Nexo.WorkModel.Application;

/// <summary>Lists the tasks of a version (with their declared inputs), ordered for presentation.</summary>
public sealed record ListVersionTasksQuery(Guid ProcessId, Guid VersionId) : IQuery<IReadOnlyList<WorkTaskDto>>;
