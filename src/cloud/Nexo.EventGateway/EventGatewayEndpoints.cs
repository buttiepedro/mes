using System.Text.Json;

namespace Nexo.EventGateway;

public static class EventGatewayEndpoints
{
    public static IEndpointRouteBuilder MapEventGatewayEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1").WithTags("Event Gateway").RequireAuthorization();

        // Ingesta de eventos del rules-engine → entrega a HEXA (firma + reintentos).
        group.MapPost("/events", async (JsonElement body, DeliveryService delivery, CancellationToken ct) =>
        {
            var items = body.ValueKind == JsonValueKind.Array ? body.EnumerateArray().ToList() : new List<JsonElement> { body };
            foreach (var e in items) await delivery.DeliverAsync(e, ct);
            return Results.Accepted(value: new { accepted = items.Count });
        });

        group.MapGet("/deliveries", (DeliveryService delivery) => Results.Ok(delivery.Deliveries));

        return app;
    }
}
