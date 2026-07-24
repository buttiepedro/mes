using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;
using Nexo.MasterData.Domain;

namespace Nexo.MasterData.Application;

public sealed class CreateUomCommandHandler : ICommandHandler<CreateUomCommand, Guid>
{
    private readonly IMasterDataDbContext _dbContext;

    public CreateUomCommandHandler(IMasterDataDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<Guid>> Handle(CreateUomCommand request, CancellationToken cancellationToken)
    {
        if (!MasterDataWireValues.TryParseMagnitude(request.Magnitude, out var magnitude))
        {
            return Result<Guid>.Failure(new Error(
                "MasterData.Uom.MagnitudeInvalid",
                $"Unknown magnitude '{request.Magnitude}'."));
        }

        var existing = await _dbContext.FindUomByCodeAsync(request.Code, cancellationToken);
        if (existing is not null)
        {
            return Result<Guid>.Failure(new Error(
                "MasterData.Uom.CodeConflict",
                $"A unit of measure with code '{request.Code}' already exists in this tenant."));
        }

        var uom = Uom.Create(
            request.Code,
            request.Name,
            request.Symbol,
            magnitude,
            request.FactorToBase,
            request.IsBase,
            request.Decimals,
            MasterGovernance.Local,
            request.ExternalRef);

        _dbContext.AddUom(uom);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(uom.Id);
    }
}
