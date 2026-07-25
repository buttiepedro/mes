using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;
using Nexo.Execution.Domain;

namespace Nexo.Execution.Application;

public sealed class CancelExecutionCommandHandler : ICommandHandler<CancelExecutionCommand, ExecutionDto>
{
    private readonly IExecutionDbContext _dbContext;

    public CancelExecutionCommandHandler(IExecutionDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<ExecutionDto>> Handle(CancelExecutionCommand request, CancellationToken cancellationToken)
    {
        var execution = await _dbContext.FindExecutionAsync(request.ExecutionId, cancellationToken);
        if (execution is null)
        {
            return Result<ExecutionDto>.Failure(ExecutionErrors.ExecutionNotFound(request.ExecutionId.ToString()));
        }

        var result = execution.Cancel(request.Reason);
        if (result.IsFailure)
        {
            return Result<ExecutionDto>.Failure(result.Error);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result<ExecutionDto>.Success(execution.ToDto());
    }
}
