using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;
using Nexo.MasterData.Domain;

namespace Nexo.MasterData.Application;

public sealed class ListCustomersQueryHandler : IQueryHandler<ListCustomersQuery, IReadOnlyList<CustomerDto>>
{
    private readonly IMasterDataDbContext _dbContext;

    public ListCustomersQueryHandler(IMasterDataDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<IReadOnlyList<CustomerDto>>> Handle(ListCustomersQuery request, CancellationToken cancellationToken)
    {
        MasterStatus? status = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!MasterDataWireValues.TryParseStatus(request.Status, out var parsedStatus))
            {
                return Result<IReadOnlyList<CustomerDto>>.Failure(new Error(
                    "MasterData.Status.Invalid",
                    $"Unknown status '{request.Status}'. Expected one of: active, archived."));
            }

            status = parsedStatus;
        }

        var customers = await _dbContext.ListCustomersAsync(
            status,
            request.Search,
            PagingDefaults.Clamp(request.Limit),
            PagingDefaults.NormalizeOffset(request.Offset),
            cancellationToken);

        return Result<IReadOnlyList<CustomerDto>>.Success(customers.Select(customer => customer.ToDto()).ToArray());
    }
}
