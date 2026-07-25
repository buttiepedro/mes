using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;
using Nexo.Execution.Domain;

namespace Nexo.Execution.Application;

public sealed class BlockTaskCommandHandler : ICommandHandler<BlockTaskCommand>
{
    private readonly IExecutionDbContext _dbContext;

    public BlockTaskCommandHandler(IExecutionDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result> Handle(BlockTaskCommand request, CancellationToken cancellationToken)
    {
        if (!ExecutionWireValues.TryParseBlockCause(request.Cause, out var cause))
        {
            return Result.Failure(new Error(
                "Execution.BlockCauseInvalid",
                $"Unknown block cause '{request.Cause}'. Expected one of: input, resource, approval, quality."));
        }

        var execution = await _dbContext.FindExecutionByTaskRunAsync(request.TaskRunId, cancellationToken);
        if (execution is null)
        {
            return Result.Failure(ExecutionErrors.TaskRunNotFound(request.TaskRunId.ToString()));
        }

        var result = execution.BlockTask(request.TaskRunId, cause, request.ReasonCodeId);
        if (result.IsFailure)
        {
            return result;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
