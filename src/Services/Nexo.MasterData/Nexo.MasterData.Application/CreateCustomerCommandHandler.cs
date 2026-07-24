using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;
using Nexo.MasterData.Domain;

namespace Nexo.MasterData.Application;

public sealed class CreateCustomerCommandHandler : ICommandHandler<CreateCustomerCommand, Guid>
{
    private readonly IMasterDataDbContext _dbContext;

    public CreateCustomerCommandHandler(IMasterDataDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<Guid>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        if (await _dbContext.CustomerCodeExistsAsync(request.Code, cancellationToken))
        {
            return Result<Guid>.Failure(new Error(
                "MasterData.Customer.CodeConflict",
                $"A customer with code '{request.Code}' already exists in this tenant."));
        }

        var customer = Customer.Create(
            request.Code,
            request.LegalName,
            request.TaxId,
            request.Contact,
            request.Notes,
            MasterGovernance.Local,
            request.ExternalRef);

        _dbContext.AddCustomer(customer);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(customer.Id);
    }
}
