using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nexo.MesApi.Application;
using Nexo.MesApi.Domain;

namespace Nexo.MesApi.Api;

/// <summary>ABM de dispositivos de señal (MQTT) y sus señales/tags.</summary>
public static class SignalEndpoints
{
    public static IEndpointRouteBuilder MapSignalEndpoints(this IEndpointRouteBuilder app)
    {
        var devices = app.MapGroup("/v1/signal-devices").WithTags("Config · Signals").RequireAuthorization();

        devices.MapGet("/", async (IMesConfigDbContext db, CancellationToken ct) =>
        {
            var list = await db.SignalDevices.OrderBy(d => d.Code).ToListAsync(ct);
            return Results.Ok(list.Select(d => new
            {
                d.Id, d.LocationNodeId, d.Code, d.Name, protocol = d.Protocol.ToString(),
                config = d.Config is null ? null : (JsonElement?)JsonDocument.Parse(d.Config).RootElement,
            }));
        });

        devices.MapPost("/", async (CreateSignalDeviceRequest req, IMesConfigDbContext db, CancellationToken ct) =>
        {
            if (!Enum.TryParse<SignalProtocol>(req.Protocol, true, out var protocol))
            {
                return Results.BadRequest(new { error = $"protocol inválido: '{req.Protocol}' (mqtt|opcua|modbus|s7)" });
            }

            var device = new SignalDevice(Guid.NewGuid(), req.LocationNodeId, req.Code, req.Name, protocol)
            {
                Config = req.Config?.GetRawText(),
            };
            db.SignalDevices.Add(device);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/v1/signal-devices/{device.Id}", new { device.Id });
        });

        devices.MapGet("/{deviceId:guid}/signals", async (Guid deviceId, IMesConfigDbContext db, CancellationToken ct) =>
            Results.Ok(await db.Signals.Where(s => s.DeviceId == deviceId).OrderBy(s => s.Code).Select(s => new
            {
                s.Id, s.DeviceId, s.Code, s.Name, s.MqttTopic, s.JsonPath,
                valueType = s.ValueType.ToString(), s.Unit, persistence = s.Persistence.ToString(),
            }).ToListAsync(ct)));

        devices.MapPost("/{deviceId:guid}/signals", async (Guid deviceId, CreateSignalRequest req, IMesConfigDbContext db, CancellationToken ct) =>
        {
            if (!Enum.TryParse<SignalValueType>(req.ValueType, true, out var valueType))
            {
                return Results.BadRequest(new { error = $"valueType inválido: '{req.ValueType}' (number|bool|string)" });
            }

            var persistence = SignalPersistence.EventsOnly;
            if (req.Persistence is not null && !Enum.TryParse(req.Persistence.Replace("_", string.Empty), true, out persistence))
            {
                return Results.BadRequest(new { error = $"persistence inválido: '{req.Persistence}' (events_only|timeseries → EventsOnly|Timeseries)" });
            }

            var signal = new Signal(Guid.NewGuid(), deviceId, req.Code, req.Name, req.MqttTopic, valueType)
            {
                JsonPath = req.JsonPath,
                Unit = req.Unit,
                Persistence = persistence,
            };
            db.Signals.Add(signal);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/v1/signal-devices/{deviceId}/signals/{signal.Id}", new { signal.Id });
        });

        return app;
    }
}

public sealed record CreateSignalDeviceRequest(
    Guid LocationNodeId, string Code, string Name, string Protocol, JsonElement? Config = null);

public sealed record CreateSignalRequest(
    string Code, string Name, string MqttTopic, string ValueType,
    string? JsonPath = null, string? Unit = null, string? Persistence = null);
