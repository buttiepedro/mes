using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;

namespace Nexo.WorkModel.Application;

public sealed class RemoveTaskCommandHandler : ICommandHandler<RemoveTaskCommand>
{
    private readonly IWorkModelDbContext _dbContext;

    public RemoveTaskCommandHandler(IWorkModelDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result> Handle(RemoveTaskCommand request, CancellationToken cancellationToken)
    {
        var version = await _dbContext.FindVersionAsync(request.VersionId, cancellationToken);
        if (version is null || version.ProcessId != request.ProcessId)
        {
            return Result.Failure(new Error(
                "WorkModel.Version.NotFound",
                $"Version '{request.VersionId}' was not found for process '{request.ProcessId}'."));
        }

        var removed = version.RemoveTask(request.TaskId);
        if (removed.IsFailure)
        {
            return removed;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
