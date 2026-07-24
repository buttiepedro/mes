using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;

namespace Nexo.MasterData.Application;

public sealed class GetCustomerQueryHandler : IQueryHandler<GetCustomerQuery, CustomerDto>
{
    private readonly IMasterDataDbContext _dbContext;

    public GetCustomerQueryHandler(IMasterDataDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<CustomerDto>> Handle(GetCustomerQuery request, CancellationToken cancellationToken)
    {
        var customer = await _dbContext.FindCustomerAsync(request.CustomerId, cancellationToken);

        return customer is null
            ? Result<CustomerDto>.Failure(new Error(
                "MasterData.Customer.NotFound",
                $"Customer '{request.CustomerId}' was not found."))
            : Result<CustomerDto>.Success(customer.ToDto());
    }
}
