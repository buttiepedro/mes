using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;
using Nexo.Execution.Domain;

namespace Nexo.Execution.Application;

public sealed class CloseExecutionCommandHandler : ICommandHandler<CloseExecutionCommand, ExecutionDto>
{
    private readonly IExecutionDbContext _dbContext;

    public CloseExecutionCommandHandler(IExecutionDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<ExecutionDto>> Handle(CloseExecutionCommand request, CancellationToken cancellationToken)
    {
        if (!ExecutionWireValues.TryParseCloseKind(request.Mode, out var kind))
        {
            return Result<ExecutionDto>.Failure(new Error(
                "Execution.CloseKindInvalid",
                $"Unknown close mode '{request.Mode}'. Expected one of: normal, partial, forced."));
        }

        var execution = await _dbContext.FindExecutionAsync(request.ExecutionId, cancellationToken);
        if (execution is null)
        {
            return Result<ExecutionDto>.Failure(ExecutionErrors.ExecutionNotFound(request.ExecutionId.ToString()));
        }

        var result = execution.Close(kind, request.Reason);
        if (result.IsFailure)
        {
            return Result<ExecutionDto>.Failure(result.Error);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result<ExecutionDto>.Success(execution.ToDto());
    }
}
