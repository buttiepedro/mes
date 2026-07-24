using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;

namespace Nexo.MasterData.Application;

public sealed class ArchiveItemCommandHandler : ICommandHandler<ArchiveItemCommand, ItemDto>
{
    private readonly IMasterDataDbContext _dbContext;

    public ArchiveItemCommandHandler(IMasterDataDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<ItemDto>> Handle(ArchiveItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _dbContext.FindItemAsync(request.ItemId, cancellationToken);
        if (item is null)
        {
            return Result<ItemDto>.Failure(new Error(
                "MasterData.Item.NotFound",
                $"Item '{request.ItemId}' was not found."));
        }

        if (item.IsArchived)
        {
            return Result<ItemDto>.Failure(new Error(
                "MasterData.Item.ArchivedConflict",
                $"Item '{item.Code}' is already archived."));
        }

        item.Archive();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<ItemDto>.Success(item.ToDto());
    }
}
