using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nexo.MesApi.Application;
using Nexo.MesApi.Domain;

namespace Nexo.MesApi.Api;

/// <summary>ABM de cámaras (fuente de visión) y sus zonas (regiones de interés poligonales).</summary>
public static class CameraEndpoints
{
    public static IEndpointRouteBuilder MapCameraEndpoints(this IEndpointRouteBuilder app)
    {
        var cameras = app.MapGroup("/v1/cameras").WithTags("Config · Cameras").RequireAuthorization();

        cameras.MapGet("/", async (IMesConfigDbContext db, CancellationToken ct) =>
            Results.Ok(await db.Cameras.OrderBy(c => c.Code).Select(c => new
            {
                c.Id, c.LocationNodeId, c.Code, c.Name, c.StreamUrl,
                transport = c.Transport.ToString(), c.Fps, c.Resolution, status = c.Status.ToString(),
            }).ToListAsync(ct)));

        cameras.MapPost("/", async (CreateCameraRequest req, IMesConfigDbContext db, CancellationToken ct) =>
        {
            if (!Enum.TryParse<CameraTransport>(req.Transport, true, out var transport))
            {
                return Results.BadRequest(new { error = $"transport inválido: '{req.Transport}' (rtsp|usb)" });
            }

            var camera = new Camera(Guid.NewGuid(), req.LocationNodeId, req.Code, req.Name, req.StreamUrl, transport)
            {
                Fps = req.Fps ?? 10,
                Resolution = req.Resolution,
            };
            db.Cameras.Add(camera);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/v1/cameras/{camera.Id}", new { camera.Id });
        });

        cameras.MapGet("/{cameraId:guid}/zones", async (Guid cameraId, IMesConfigDbContext db, CancellationToken ct) =>
        {
            var zones = await db.Zones.Where(z => z.CameraId == cameraId).OrderBy(z => z.Code).ToListAsync(ct);
            return Results.Ok(zones.Select(z => new
            {
                z.Id, z.CameraId, z.Code, z.Name, z.Purpose,
                polygon = JsonDocument.Parse(z.Polygon).RootElement,
            }));
        });

        cameras.MapPost("/{cameraId:guid}/zones", async (Guid cameraId, CreateZoneRequest req, IMesConfigDbContext db, CancellationToken ct) =>
        {
            var zone = new Zone(Guid.NewGuid(), cameraId, req.Code, req.Name, req.Polygon.GetRawText())
            {
                Purpose = req.Purpose,
            };
            db.Zones.Add(zone);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/v1/cameras/{cameraId}/zones/{zone.Id}", new { zone.Id });
        });

        return app;
    }
}

public sealed record CreateCameraRequest(
    Guid LocationNodeId, string Code, string Name, string StreamUrl, string Transport,
    int? Fps = null, string? Resolution = null);

/// <summary><c>Polygon</c> = lista de puntos [x,y] normalizados 0..1 (JSON).</summary>
public sealed record CreateZoneRequest(string Code, string Name, JsonElement Polygon, string? Purpose = null);
