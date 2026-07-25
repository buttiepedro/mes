using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;
using Nexo.Execution.Domain;

namespace Nexo.Execution.Application;

public sealed class UnblockTaskCommandHandler : ICommandHandler<UnblockTaskCommand>
{
    private readonly IExecutionDbContext _dbContext;

    public UnblockTaskCommandHandler(IExecutionDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result> Handle(UnblockTaskCommand request, CancellationToken cancellationToken)
    {
        var execution = await _dbContext.FindExecutionByTaskRunAsync(request.TaskRunId, cancellationToken);
        if (execution is null)
        {
            return Result.Failure(ExecutionErrors.TaskRunNotFound(request.TaskRunId.ToString()));
        }

        var result = execution.UnblockTask(request.TaskRunId, request.Resolution);
        if (result.IsFailure)
        {
            return result;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
