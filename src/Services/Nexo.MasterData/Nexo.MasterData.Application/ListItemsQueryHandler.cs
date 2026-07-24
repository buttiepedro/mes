using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;
using Nexo.MasterData.Domain;

namespace Nexo.MasterData.Application;

public sealed class ListItemsQueryHandler : IQueryHandler<ListItemsQuery, IReadOnlyList<ItemDto>>
{
    private readonly IMasterDataDbContext _dbContext;

    public ListItemsQueryHandler(IMasterDataDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<IReadOnlyList<ItemDto>>> Handle(ListItemsQuery request, CancellationToken cancellationToken)
    {
        ItemRole? role = null;
        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            if (!MasterDataWireValues.TryParseRole(request.Role, out var parsedRole))
            {
                return Result<IReadOnlyList<ItemDto>>.Failure(new Error(
                    "MasterData.Item.RoleInvalid",
                    $"Unknown item role '{request.Role}'. Expected one of: product, input."));
            }

            role = parsedRole;
        }

        MasterStatus? status = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!MasterDataWireValues.TryParseStatus(request.Status, out var parsedStatus))
            {
                return Result<IReadOnlyList<ItemDto>>.Failure(new Error(
                    "MasterData.Status.Invalid",
                    $"Unknown status '{request.Status}'. Expected one of: active, archived."));
            }

            status = parsedStatus;
        }

        var items = await _dbContext.ListItemsAsync(
            role,
            status,
            request.Search,
            PagingDefaults.Clamp(request.Limit),
            PagingDefaults.NormalizeOffset(request.Offset),
            cancellationToken);

        return Result<IReadOnlyList<ItemDto>>.Success(items.Select(item => item.ToDto()).ToArray());
    }
}
