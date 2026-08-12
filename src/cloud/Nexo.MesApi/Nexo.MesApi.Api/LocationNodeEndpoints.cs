using Microsoft.EntityFrameworkCore;
using Nexo.MesApi.Application;
using Nexo.MesApi.Domain;

namespace Nexo.MesApi.Api;

/// <summary>ABM de la jerarquía de planta (Planta→Sector→Línea→Estación).</summary>
public static class LocationNodeEndpoints
{
    public static IEndpointRouteBuilder MapLocationNodeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/location-nodes").WithTags("Config · Location").RequireAuthorization();

        group.MapGet("/", async (IMesConfigDbContext db, CancellationToken ct) =>
            Results.Ok(await db.LocationNodes
                .OrderBy(n => n.Code)
                .Select(n => new { n.Id, n.ParentId, level = n.Level.ToString(), n.Code, n.Name })
                .ToListAsync(ct)));

        group.MapPost("/", async (CreateLocationNodeRequest req, IMesConfigDbContext db, CancellationToken ct) =>
        {
            if (!Enum.TryParse<LocationLevel>(req.Level, ignoreCase: true, out var level))
            {
                return Results.BadRequest(new { error = $"level inválido: '{req.Level}' (site|area|line|station)" });
            }

            var node = new LocationNode(Guid.NewGuid(), req.ParentId, level, req.Code, req.Name);
            db.LocationNodes.Add(node);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/v1/location-nodes/{node.Id}", new { node.Id });
        });

        return app;
    }
}

public sealed record CreateLocationNodeRequest(string Level, string Code, string Name, Guid? ParentId = null);
