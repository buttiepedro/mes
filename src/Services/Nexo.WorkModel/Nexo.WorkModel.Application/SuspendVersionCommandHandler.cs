using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;

namespace Nexo.WorkModel.Application;

public sealed class SuspendVersionCommandHandler : ICommandHandler<SuspendVersionCommand, ProcessVersionDto>
{
    private readonly IWorkModelDbContext _dbContext;

    public SuspendVersionCommandHandler(IWorkModelDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<ProcessVersionDto>> Handle(SuspendVersionCommand request, CancellationToken cancellationToken)
    {
        var process = await _dbContext.FindProcessAsync(request.ProcessId, cancellationToken);
        if (process is null)
        {
            return Result<ProcessVersionDto>.Failure(new Error(
                "WorkModel.Process.NotFound",
                $"Process '{request.ProcessId}' was not found."));
        }

        var version = await _dbContext.FindVersionAsync(request.VersionId, cancellationToken);
        if (version is null || version.ProcessId != process.Id)
        {
            return Result<ProcessVersionDto>.Failure(new Error(
                "WorkModel.Version.NotFound",
                $"Version '{request.VersionId}' was not found for process '{request.ProcessId}'."));
        }

        var suspended = process.SuspendVersion(version, request.Reason);
        if (suspended.IsFailure)
        {
            return Result<ProcessVersionDto>.Failure(suspended.Error);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<ProcessVersionDto>.Success(version.ToDto());
    }
}
