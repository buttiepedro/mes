using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;

namespace Nexo.WorkModel.Application;

public sealed class ListVersionTasksQueryHandler : IQueryHandler<ListVersionTasksQuery, IReadOnlyList<WorkTaskDto>>
{
    private readonly IWorkModelDbContext _dbContext;

    public ListVersionTasksQueryHandler(IWorkModelDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<IReadOnlyList<WorkTaskDto>>> Handle(ListVersionTasksQuery request, CancellationToken cancellationToken)
    {
        var version = await _dbContext.FindVersionAsync(request.VersionId, cancellationToken);
        if (version is null || version.ProcessId != request.ProcessId)
        {
            return Result<IReadOnlyList<WorkTaskDto>>.Failure(new Error(
                "WorkModel.Version.NotFound",
                $"Version '{request.VersionId}' was not found for process '{request.ProcessId}'."));
        }

        var tasks = version.Tasks
            .OrderBy(task => task.DisplaySeq)
            .ThenBy(task => task.Code)
            .Select(task => task.ToDto())
            .ToArray();

        return Result<IReadOnlyList<WorkTaskDto>>.Success(tasks);
    }
}
