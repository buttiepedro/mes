using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;
using Nexo.Execution.Domain;

namespace Nexo.Execution.Application;

public sealed class GetExecutionSnapshotQueryHandler : IQueryHandler<GetExecutionSnapshotQuery, ExecutionSnapshotDto>
{
    private readonly IExecutionDbContext _dbContext;

    public GetExecutionSnapshotQueryHandler(IExecutionDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<ExecutionSnapshotDto>> Handle(GetExecutionSnapshotQuery request, CancellationToken cancellationToken)
    {
        var execution = await _dbContext.FindExecutionAsync(request.ExecutionId, cancellationToken);

        return execution is null
            ? Result<ExecutionSnapshotDto>.Failure(ExecutionErrors.ExecutionNotFound(request.ExecutionId.ToString()))
            : Result<ExecutionSnapshotDto>.Success(execution.ToSnapshotDto());
    }
}
