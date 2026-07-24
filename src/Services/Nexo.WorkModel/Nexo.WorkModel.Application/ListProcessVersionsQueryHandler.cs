using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;

namespace Nexo.WorkModel.Application;

public sealed class ListProcessVersionsQueryHandler : IQueryHandler<ListProcessVersionsQuery, IReadOnlyList<ProcessVersionDto>>
{
    private readonly IWorkModelDbContext _dbContext;

    public ListProcessVersionsQueryHandler(IWorkModelDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<IReadOnlyList<ProcessVersionDto>>> Handle(ListProcessVersionsQuery request, CancellationToken cancellationToken)
    {
        var process = await _dbContext.FindProcessAsync(request.ProcessId, cancellationToken);
        if (process is null)
        {
            return Result<IReadOnlyList<ProcessVersionDto>>.Failure(new Error(
                "WorkModel.Process.NotFound",
                $"Process '{request.ProcessId}' was not found."));
        }

        var versions = await _dbContext.ListVersionsAsync(process.Id, cancellationToken);

        return Result<IReadOnlyList<ProcessVersionDto>>.Success(
            versions.Select(version => version.ToDto()).ToArray());
    }
}
