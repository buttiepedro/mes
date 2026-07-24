using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;

namespace Nexo.WorkModel.Application;

public sealed class GetPublishedVersionQueryHandler : IQueryHandler<GetPublishedVersionQuery, ProcessVersionGraphDto>
{
    private readonly IWorkModelDbContext _dbContext;

    public GetPublishedVersionQueryHandler(IWorkModelDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<ProcessVersionGraphDto>> Handle(GetPublishedVersionQuery request, CancellationToken cancellationToken)
    {
        var version = await _dbContext.FindPublishedVersionAsync(request.ProcessId, cancellationToken);

        return version is null
            ? Result<ProcessVersionGraphDto>.Failure(new Error(
                "WorkModel.Version.NotFound",
                $"Process '{request.ProcessId}' has no published version."))
            : Result<ProcessVersionGraphDto>.Success(version.ToGraphDto());
    }
}
