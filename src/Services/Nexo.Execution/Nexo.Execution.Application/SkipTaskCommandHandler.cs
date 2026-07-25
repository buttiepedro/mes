using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;
using Nexo.Execution.Domain;

namespace Nexo.Execution.Application;

public sealed class SkipTaskCommandHandler : ICommandHandler<SkipTaskCommand>
{
    private readonly IExecutionDbContext _dbContext;

    public SkipTaskCommandHandler(IExecutionDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result> Handle(SkipTaskCommand request, CancellationToken cancellationToken)
    {
        var execution = await _dbContext.FindExecutionByTaskRunAsync(request.TaskRunId, cancellationToken);
        if (execution is null)
        {
            return Result.Failure(ExecutionErrors.TaskRunNotFound(request.TaskRunId.ToString()));
        }

        var result = execution.SkipTask(request.TaskRunId, request.Reason, request.AuthorizedBy);
        if (result.IsFailure)
        {
            return result;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
