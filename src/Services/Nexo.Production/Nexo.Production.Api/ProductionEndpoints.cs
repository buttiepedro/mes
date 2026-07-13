using MediatR;
using Nexo.Production.Application;

namespace Nexo.Production.Api;

/// <summary>Minimal API endpoints for the Production slice under <c>/v1/production</c>.</summary>
public static class ProductionEndpoints
{
    public static IEndpointRouteBuilder MapProductionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/production").WithTags("Production");

        group.MapPost("/records", RegisterProductionAsync)
            .WithName("RegisterProduction")
            .RequireAuthorization("production.write");

        group.MapPost("/runs/{runId:guid}:close", CloseRunAsync)
            .WithName("CloseRun")
            .RequireAuthorization("production.write");

        group.MapGet("/runs/{runId:guid}", GetRunProductionAsync)
            .WithName("GetRunProduction")
            .RequireAuthorization("production.read");

        return app;
    }

    private static async Task<IResult> RegisterProductionAsync(
        RegisterProductionRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RegisterProductionCommand(
                request.RunId,
                request.GoodQty,
                request.ScrapQty,
                request.OperatorId,
                request.Source),
            cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/v1/production/records/{result.Value}", new { id = result.Value })
            : result.ToProblem();
    }

    private static async Task<IResult> CloseRunAsync(
        Guid runId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CloseRunCommand(runId), cancellationToken);

        return result.IsSuccess ? Results.NoContent() : result.ToProblem();
    }

    private static async Task<IResult> GetRunProductionAsync(
        Guid runId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetRunProductionQuery(runId), cancellationToken);

        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem();
    }
}
