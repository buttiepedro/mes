using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;

namespace Nexo.MasterData.Application;

public sealed class GetItemQueryHandler : IQueryHandler<GetItemQuery, ItemDto>
{
    private readonly IMasterDataDbContext _dbContext;

    public GetItemQueryHandler(IMasterDataDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<ItemDto>> Handle(GetItemQuery request, CancellationToken cancellationToken)
    {
        var item = await _dbContext.FindItemAsync(request.ItemId, cancellationToken);

        return item is null
            ? Result<ItemDto>.Failure(new Error(
                "MasterData.Item.NotFound",
                $"Item '{request.ItemId}' was not found."))
            : Result<ItemDto>.Success(item.ToDto());
    }
}
