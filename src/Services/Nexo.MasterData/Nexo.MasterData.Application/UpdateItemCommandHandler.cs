using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;

namespace Nexo.MasterData.Application;

public sealed class UpdateItemCommandHandler : ICommandHandler<UpdateItemCommand, ItemDto>
{
    private readonly IMasterDataDbContext _dbContext;

    public UpdateItemCommandHandler(IMasterDataDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<ItemDto>> Handle(UpdateItemCommand request, CancellationToken cancellationToken)
    {
        var roles = MasterDataWireValues.ParseRoles(request.Roles);
        if (roles is null || roles.Count == 0)
        {
            return Result<ItemDto>.Failure(new Error(
                "MasterData.Item.RolesInvalid",
                "Roles must contain at least one of: product, input."));
        }

        if (!MasterDataWireValues.TryParseTracking(request.Tracking, out var tracking))
        {
            return Result<ItemDto>.Failure(new Error(
                "MasterData.Item.TrackingInvalid",
                $"Unknown tracking mode '{request.Tracking}'."));
        }

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
                $"Item '{item.Code}' is archived and cannot be updated."));
        }

        item.Update(
            request.Name,
            roles,
            tracking,
            request.Category,
            request.Family,
            request.IdealCycleTime,
            request.DefaultProcessId,
            request.QualitySpecs);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<ItemDto>.Success(item.ToDto());
    }
}
