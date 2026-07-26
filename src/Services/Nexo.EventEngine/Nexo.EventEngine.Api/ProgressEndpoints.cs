namespace Nexo.EventEngine.Api;

/// <summary>Minimal API endpoints exposing the execution-progress read model (Capa 4).</summary>
public static class ProgressEndpoints
{
    public static IEndpointRouteBuilder MapProgressEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/v1/executions")
            .WithTags("Event Engine · Progress")
            .RequireAuthorization();

        group.MapGet("/progress", (ExecutionProgressProjection projection) =>
                Results.Ok(projection.All()))
            .WithName("ListExecutionProgress");

        group.MapGet("/{executionId:guid}/progress", (Guid executionId, ExecutionProgressProjection projection) =>
            {
                var progress = projection.Get(executionId);
                return progress is null ? Results.NotFound() : Results.Ok(progress);
            })
            .WithName("GetExecutionProgress");

        return app;
    }
}
