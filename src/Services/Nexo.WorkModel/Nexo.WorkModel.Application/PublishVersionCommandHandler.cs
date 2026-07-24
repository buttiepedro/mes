using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;

namespace Nexo.WorkModel.Application;

public sealed class PublishVersionCommandHandler : ICommandHandler<PublishVersionCommand, ProcessVersionDto>
{
    private readonly IWorkModelDbContext _dbContext;

    public PublishVersionCommandHandler(IWorkModelDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<ProcessVersionDto>> Handle(PublishVersionCommand request, CancellationToken cancellationToken)
    {
        var process = await _dbContext.FindProcessAsync(request.ProcessId, cancellationToken);
        if (process is null)
        {
            return Result<ProcessVersionDto>.Failure(new Error(
                "WorkModel.Process.NotFound",
                $"Process '{request.ProcessId}' was not found."));
        }

        // The whole graph is loaded because publishing runs the integral validation over it.
        var version = await _dbContext.FindVersionAsync(request.VersionId, cancellationToken);
        if (version is null || version.ProcessId != process.Id)
        {
            return Result<ProcessVersionDto>.Failure(new Error(
                "WorkModel.Version.NotFound",
                $"Version '{request.VersionId}' was not found for process '{request.ProcessId}'."));
        }

        // The process is the aggregate that owns "one published version at a time" (CB15).
        var published = process.PublishVersion(version);
        if (published.IsFailure)
        {
            return Result<ProcessVersionDto>.Failure(published.Error);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<ProcessVersionDto>.Success(version.ToDto());
    }
}
