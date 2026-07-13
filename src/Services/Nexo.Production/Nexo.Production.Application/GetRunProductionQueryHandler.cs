using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;

namespace Nexo.Production.Application;

public sealed class GetRunProductionQueryHandler : IQueryHandler<GetRunProductionQuery, RunProductionDto>
{
    private readonly IProductionDbContext _dbContext;

    public GetRunProductionQueryHandler(IProductionDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<RunProductionDto>> Handle(GetRunProductionQuery request, CancellationToken cancellationToken)
    {
        var run = await _dbContext.FindRunAsync(request.RunId, cancellationToken);
        if (run is null)
        {
            return Result<RunProductionDto>.Failure(new Error(
                "Production.Run.NotFound",
                $"Production run '{request.RunId}' was not found."));
        }

        var totalGood = run.Records.Sum(r => r.GoodQty.Value);
        var totalScrap = run.Records.Sum(r => r.ScrapQty.Value);

        var dto = new RunProductionDto(
            run.Id,
            run.WorkOrderId,
            totalGood,
            totalScrap,
            run.Status.ToString(),
            run.Records.Count);

        return Result<RunProductionDto>.Success(dto);
    }
}
