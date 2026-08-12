using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nexo.MesApi.Application;

namespace Nexo.MesApi.Api;

/// <summary>
/// Bundle de configuración completo del tenant — lo que el <b>edge</b> hace *pull* al arrancar y cada
/// vez que cambia la config: planta, cámaras, zonas, dispositivos de señal, señales, catálogo y reglas.
/// (Más adelante se acota por sitio/planta; hoy devuelve toda la config del tenant.)
/// </summary>
public static class ConfigBundleEndpoints
{
    public static IEndpointRouteBuilder MapConfigBundleEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/config-bundle", async (IMesConfigDbContext db, CancellationToken ct) =>
        {
            static JsonElement? Json(string? s) => s is null ? null : JsonDocument.Parse(s).RootElement;

            var locationNodes = await db.LocationNodes.OrderBy(n => n.Code).ToListAsync(ct);
            var cameras = await db.Cameras.OrderBy(c => c.Code).ToListAsync(ct);
            var zones = await db.Zones.OrderBy(z => z.Code).ToListAsync(ct);
            var devices = await db.SignalDevices.OrderBy(d => d.Code).ToListAsync(ct);
            var signals = await db.Signals.OrderBy(s => s.Code).ToListAsync(ct);
            var classes = await db.DetectionClasses.OrderBy(c => c.Code).ToListAsync(ct);
            var models = await db.VisionModels.OrderBy(m => m.Version).ToListAsync(ct);
            var rules = await db.Rules.OrderBy(r => r.Code).ToListAsync(ct);

            return Results.Ok(new
            {
                locationNodes = locationNodes.Select(n => new { n.Id, n.ParentId, level = n.Level.ToString(), n.Code, n.Name }),
                cameras = cameras.Select(c => new { c.Id, c.LocationNodeId, c.Code, c.Name, c.StreamUrl, transport = c.Transport.ToString(), c.Fps, c.Resolution, status = c.Status.ToString(), adjacentCameras = Json(c.AdjacentCameras) }),
                zones = zones.Select(z => new { z.Id, z.CameraId, z.Code, z.Name, z.Purpose, polygon = Json(z.Polygon) }),
                signalDevices = devices.Select(d => new { d.Id, d.LocationNodeId, d.Code, d.Name, protocol = d.Protocol.ToString(), config = Json(d.Config) }),
                signals = signals.Select(s => new { s.Id, s.DeviceId, s.Code, s.Name, s.MqttTopic, s.JsonPath, valueType = s.ValueType.ToString(), s.Unit, persistence = s.Persistence.ToString() }),
                detectionClasses = classes.Select(c => new { c.Id, kind = c.Kind.ToString(), c.Code, c.Name, scope = c.Scope.ToString() }),
                visionModels = models.Select(m => new { m.Id, kind = m.Kind.ToString(), m.Version, m.ArtifactRef, providesClasses = Json(m.ProvidesClasses) }),
                rules = rules.Select(r => new { r.Id, r.Code, r.Name, r.Enabled, r.ScopeLocationNodeId, r.CooldownSeconds, trigger = Json(r.Trigger), emit = Json(r.Emit) }),
            });
        }).WithTags("Config · Bundle").RequireAuthorization();

        return app;
    }
}
