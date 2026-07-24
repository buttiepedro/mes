using Nexo.BuildingBlocks.Application;

namespace Nexo.WorkModel.Application;

/// <summary>Version history of a process, newest first (headers only, no graph).</summary>
public sealed record ListProcessVersionsQuery(Guid ProcessId) : IQuery<IReadOnlyList<ProcessVersionDto>>;
