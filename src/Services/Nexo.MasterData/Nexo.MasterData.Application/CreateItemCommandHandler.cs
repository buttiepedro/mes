using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;
using Nexo.MasterData.Domain;

namespace Nexo.MasterData.Application;

public sealed class CreateItemCommandHandler : ICommandHandler<CreateItemCommand, Guid>
{
    private readonly IMasterDataDbContext _dbContext;

    public CreateItemCommandHandler(IMasterDataDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<Guid>> Handle(CreateItemCommand request, CancellationToken cancellationToken)
    {
        var roles = MasterDataWireValues.ParseRoles(request.Roles);
        if (roles is null || roles.Count == 0)
        {
            return Result<Guid>.Failure(new Error(
                "MasterData.Item.RolesInvalid",
                "Roles must contain at least one of: product, input."));
        }

        if (!MasterDataWireValues.TryParseTracking(request.Tracking, out var tracking))
        {
            return Result<Guid>.Failure(new Error(
                "MasterData.Item.TrackingInvalid",
                $"Unknown tracking mode '{request.Tracking}'."));
        }

        var baseUom = await _dbContext.FindUomByCodeAsync(request.BaseUom, cancellationToken);
        if (baseUom is null)
        {
            return Result<Guid>.Failure(new Error(
                "MasterData.Uom.NotFound",
                $"Base unit of measure '{request.BaseUom}' was not found."));
        }

        if (await _dbContext.ItemCodeExistsAsync(request.Code, cancellationToken))
        {
            return Result<Guid>.Failure(new Error(
                "MasterData.Item.CodeConflict",
                $"An item with code '{request.Code}' already exists in this tenant."));
        }

        var item = Item.Create(
            request.Code,
            request.Name,
            baseUom.Id,
            roles,
            tracking,
            request.Category,
            request.Family,
            request.IdealCycleTime,
            request.DefaultProcessId,
            request.QualitySpecs,
            MasterGovernance.Local,
            request.ExternalRef);

        _dbContext.AddItem(item);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(item.Id);
    }
}
