using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;
using Nexo.Execution.Domain;

namespace Nexo.Execution.Application;

public sealed class ReportProgressCommandHandler : ICommandHandler<ReportProgressCommand>
{
    private readonly IExecutionDbContext _dbContext;

    public ReportProgressCommandHandler(IExecutionDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result> Handle(ReportProgressCommand request, CancellationToken cancellationToken)
    {
        if (!ExecutionWireValues.TryParseProgressMethod(request.Method, out var method))
        {
            return Result.Failure(new Error(
                "Execution.ProgressMethodInvalid",
                $"Unknown progress method '{request.Method}'. Expected one of: declared, quantity, checklist, time, signal."));
        }

        var execution = await _dbContext.FindExecutionByTaskRunAsync(request.TaskRunId, cancellationToken);
        if (execution is null)
        {
            return Result.Failure(ExecutionErrors.TaskRunNotFound(request.TaskRunId.ToString()));
        }

        var result = execution.ReportProgress(request.TaskRunId, method, request.ProgressPct, request.Quantity, request.TargetQuantity);
        if (result.IsFailure)
        {
            return result;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
