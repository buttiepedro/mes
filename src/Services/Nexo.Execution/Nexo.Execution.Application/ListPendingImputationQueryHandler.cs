using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;

namespace Nexo.Execution.Application;

public sealed class ListPendingImputationQueryHandler
    : IQueryHandler<ListPendingImputationQuery, IReadOnlyList<PendingImputationDto>>
{
    private readonly IExecutionDbContext _dbContext;

    public ListPendingImputationQueryHandler(IExecutionDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<IReadOnlyList<PendingImputationDto>>> Handle(ListPendingImputationQuery request, CancellationToken cancellationToken)
    {
        var rows = await _dbContext.ListPendingImputationAsync(
            PagingDefaults.Clamp(request.Limit),
            PagingDefaults.NormalizeOffset(request.Offset),
            cancellationToken);

        return Result<IReadOnlyList<PendingImputationDto>>.Success(rows.Select(row => row.ToDto()).ToArray());
    }
}
