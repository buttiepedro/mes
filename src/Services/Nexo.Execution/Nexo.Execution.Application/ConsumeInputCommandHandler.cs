using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;
using Nexo.Execution.Domain;

namespace Nexo.Execution.Application;

public sealed class ConsumeInputCommandHandler : ICommandHandler<ConsumeInputCommand, InputConsumptionDto>
{
    private readonly IExecutionDbContext _dbContext;

    public ConsumeInputCommandHandler(IExecutionDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<InputConsumptionDto>> Handle(ConsumeInputCommand request, CancellationToken cancellationToken)
    {
        if (!ExecutionWireValues.TryParseConsumptionMethod(request.Method, out var method))
        {
            return Result<InputConsumptionDto>.Failure(new Error(
                "Execution.ConsumptionMethodInvalid",
                $"Unknown consumption method '{request.Method}'. Expected one of: declared, backflush, scale, scan, adjustment."));
        }

        var execution = await _dbContext.FindExecutionAsync(request.ExecutionId, cancellationToken);
        if (execution is null)
        {
            return Result<InputConsumptionDto>.Failure(ExecutionErrors.ExecutionNotFound(request.ExecutionId.ToString()));
        }

        var consumed = execution.ConsumeInput(
            request.TaskRunId,
            request.ItemId,
            request.Quantity,
            request.UomId,
            method,
            request.PlannedQuantity,
            request.TaskInputId,
            request.BatchId,
            request.SerialId,
            request.PersonId);

        if (consumed.IsFailure)
        {
            return Result<InputConsumptionDto>.Failure(consumed.Error);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result<InputConsumptionDto>.Success(consumed.Value.ToDto());
    }
}
