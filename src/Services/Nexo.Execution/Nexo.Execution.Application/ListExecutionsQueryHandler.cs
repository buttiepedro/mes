using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;
using Nexo.Execution.Domain;

namespace Nexo.Execution.Application;

public sealed class ListExecutionsQueryHandler : IQueryHandler<ListExecutionsQuery, IReadOnlyList<ExecutionDto>>
{
    private readonly IExecutionDbContext _dbContext;

    public ListExecutionsQueryHandler(IExecutionDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<IReadOnlyList<ExecutionDto>>> Handle(ListExecutionsQuery request, CancellationToken cancellationToken)
    {
        ExecutionFlavor? flavor = null;
        if (!string.IsNullOrWhiteSpace(request.Flavor))
        {
            if (!ExecutionWireValues.TryParseFlavorFromProfile(request.Flavor, out var parsed))
            {
                return Result<IReadOnlyList<ExecutionDto>>.Failure(new Error(
                    "Execution.FlavorInvalid",
                    $"Unknown flavour '{request.Flavor}'. Expected one of: batch, project."));
            }

            flavor = parsed;
        }

        ExecutionStatus? status = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!ExecutionWireValues.TryParseExecutionStatus(request.Status, out var parsed))
            {
                return Result<IReadOnlyList<ExecutionDto>>.Failure(new Error(
                    "Execution.StatusInvalid",
                    $"Unknown execution status '{request.Status}'."));
            }

            status = parsed;
        }

        var executions = await _dbContext.ListExecutionsAsync(
            flavor,
            status,
            request.ProcessId,
            request.DueBefore,
            PagingDefaults.Clamp(request.Limit),
            PagingDefaults.NormalizeOffset(request.Offset),
            cancellationToken);

        return Result<IReadOnlyList<ExecutionDto>>.Success(executions.Select(execution => execution.ToDto()).ToArray());
    }
}
