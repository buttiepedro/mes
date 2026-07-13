using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;

namespace Nexo.Production.Application;

public sealed class CloseRunCommandHandler : ICommandHandler<CloseRunCommand>
{
    private readonly IProductionDbContext _dbContext;

    public CloseRunCommandHandler(IProductionDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result> Handle(CloseRunCommand request, CancellationToken cancellationToken)
    {
        var run = await _dbContext.FindRunAsync(request.RunId, cancellationToken);
        if (run is null)
        {
            return Result.Failure(new Error(
                "Production.Run.NotFound",
                $"Production run '{request.RunId}' was not found."));
        }

        run.Close();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
