using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;

namespace Nexo.MasterData.Application;

public sealed class GetUomQueryHandler : IQueryHandler<GetUomQuery, UomDto>
{
    private readonly IMasterDataDbContext _dbContext;

    public GetUomQueryHandler(IMasterDataDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<UomDto>> Handle(GetUomQuery request, CancellationToken cancellationToken)
    {
        var uom = await _dbContext.FindUomAsync(request.UomId, cancellationToken);

        return uom is null
            ? Result<UomDto>.Failure(new Error(
                "MasterData.Uom.NotFound",
                $"Unit of measure '{request.UomId}' was not found."))
            : Result<UomDto>.Success(uom.ToDto());
    }
}
