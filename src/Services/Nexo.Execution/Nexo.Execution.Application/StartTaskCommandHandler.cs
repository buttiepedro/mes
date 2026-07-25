using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;
using Nexo.Execution.Domain;

namespace Nexo.Execution.Application;

public sealed class StartTaskCommandHandler : ICommandHandler<StartTaskCommand>
{
    private readonly IExecutionDbContext _dbContext;

    public StartTaskCommandHandler(IExecutionDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result> Handle(StartTaskCommand request, CancellationToken cancellationToken)
    {
        var execution = await _dbContext.FindExecutionByTaskRunAsync(request.TaskRunId, cancellationToken);
        if (execution is null)
        {
            return Result.Failure(ExecutionErrors.TaskRunNotFound(request.TaskRunId.ToString()));
        }

        var result = execution.StartTask(request.TaskRunId, request.OperatorId);
        if (result.IsFailure)
        {
            return result;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
