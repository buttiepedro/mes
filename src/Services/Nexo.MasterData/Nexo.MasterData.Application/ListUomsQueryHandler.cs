using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;
using Nexo.MasterData.Domain;

namespace Nexo.MasterData.Application;

public sealed class ListUomsQueryHandler : IQueryHandler<ListUomsQuery, IReadOnlyList<UomDto>>
{
    private readonly IMasterDataDbContext _dbContext;

    public ListUomsQueryHandler(IMasterDataDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<IReadOnlyList<UomDto>>> Handle(ListUomsQuery request, CancellationToken cancellationToken)
    {
        UomMagnitude? magnitude = null;
        if (!string.IsNullOrWhiteSpace(request.Magnitude))
        {
            if (!MasterDataWireValues.TryParseMagnitude(request.Magnitude, out var parsedMagnitude))
            {
                return Result<IReadOnlyList<UomDto>>.Failure(new Error(
                    "MasterData.Uom.MagnitudeInvalid",
                    $"Unknown magnitude '{request.Magnitude}'."));
            }

            magnitude = parsedMagnitude;
        }

        MasterStatus? status = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!MasterDataWireValues.TryParseStatus(request.Status, out var parsedStatus))
            {
                return Result<IReadOnlyList<UomDto>>.Failure(new Error(
                    "MasterData.Status.Invalid",
                    $"Unknown status '{request.Status}'. Expected one of: active, archived."));
            }

            status = parsedStatus;
        }

        var uoms = await _dbContext.ListUomsAsync(
            magnitude,
            status,
            PagingDefaults.Clamp(request.Limit),
            PagingDefaults.NormalizeOffset(request.Offset),
            cancellationToken);

        return Result<IReadOnlyList<UomDto>>.Success(uoms.Select(uom => uom.ToDto()).ToArray());
    }
}
