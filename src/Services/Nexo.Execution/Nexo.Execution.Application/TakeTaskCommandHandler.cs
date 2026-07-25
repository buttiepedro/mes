using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;
using Nexo.Execution.Domain;

namespace Nexo.Execution.Application;

public sealed class TakeTaskCommandHandler : ICommandHandler<TakeTaskCommand>
{
    private readonly IExecutionDbContext _dbContext;

    public TakeTaskCommandHandler(IExecutionDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result> Handle(TakeTaskCommand request, CancellationToken cancellationToken)
    {
        if (!ExecutionWireValues.TryParseAssignmentMode(request.Mode, out var mode))
        {
            return Result.Failure(new Error(
                "Execution.AssignmentModeInvalid",
                $"Unknown assignment mode '{request.Mode}'. Expected one of: individual, crew, role_open, automatic, external."));
        }

        var execution = await _dbContext.FindExecutionByTaskRunAsync(request.TaskRunId, cancellationToken);
        if (execution is null)
        {
            return Result.Failure(ExecutionErrors.TaskRunNotFound(request.TaskRunId.ToString()));
        }

        var result = execution.TakeTask(request.TaskRunId, request.PersonId, request.RoleId, mode);
        if (result.IsFailure)
        {
            return result;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
