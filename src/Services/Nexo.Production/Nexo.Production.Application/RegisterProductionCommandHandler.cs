using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;
using Nexo.Production.Domain;

namespace Nexo.Production.Application;

public sealed class RegisterProductionCommandHandler : ICommandHandler<RegisterProductionCommand, Guid>
{
    private readonly IProductionDbContext _dbContext;

    public RegisterProductionCommandHandler(IProductionDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<Guid>> Handle(RegisterProductionCommand request, CancellationToken cancellationToken)
    {
        var run = await _dbContext.FindRunAsync(request.RunId, cancellationToken);
        if (run is null)
        {
            return Result<Guid>.Failure(new Error(
                "Production.Run.NotFound",
                $"Production run '{request.RunId}' was not found."));
        }

        if (!Enum.TryParse<ProductionSource>(request.Source, ignoreCase: true, out var source))
        {
            return Result<Guid>.Failure(new Error(
                "Production.Source.Invalid",
                $"Unknown production source '{request.Source}'."));
        }

        var record = run.Register(
            Quantity.Of(request.GoodQty),
            Quantity.Of(request.ScrapQty),
            request.OperatorId,
            source);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(record.Id);
    }
}
