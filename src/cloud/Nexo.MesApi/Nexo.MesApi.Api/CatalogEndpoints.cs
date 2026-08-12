using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nexo.MesApi.Application;
using Nexo.MesApi.Domain;

namespace Nexo.MesApi.Api;

/// <summary>Catálogo de clases de detección (objetos/acciones) y modelos de visión.</summary>
public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var classes = app.MapGroup("/v1/detection-classes").WithTags("Config · Catalog").RequireAuthorization();

        classes.MapGet("/", async (IMesConfigDbContext db, CancellationToken ct) =>
            Results.Ok(await db.DetectionClasses.OrderBy(c => c.Code).Select(c => new
            {
                c.Id, kind = c.Kind.ToString(), c.Code, c.Name, scope = c.Scope.ToString(),
            }).ToListAsync(ct)));

        classes.MapPost("/", async (CreateDetectionClassRequest req, IMesConfigDbContext db, CancellationToken ct) =>
        {
            if (!Enum.TryParse<DetectionKind>(req.Kind, true, out var kind))
            {
                return Results.BadRequest(new { error = $"kind inválido: '{req.Kind}' (object|action)" });
            }

            if (!Enum.TryParse<DetectionScope>(req.Scope, true, out var scope))
            {
                return Results.BadRequest(new { error = $"scope inválido: '{req.Scope}' (shared|tenant)" });
            }

            var cls = new DetectionClass(Guid.NewGuid(), kind, req.Code, req.Name, scope);
            db.DetectionClasses.Add(cls);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/v1/detection-classes/{cls.Id}", new { cls.Id });
        });

        var models = app.MapGroup("/v1/vision-models").WithTags("Config · Catalog").RequireAuthorization();

        models.MapGet("/", async (IMesConfigDbContext db, CancellationToken ct) =>
        {
            var list = await db.VisionModels.OrderBy(m => m.Version).ToListAsync(ct);
            return Results.Ok(list.Select(m => new
            {
                m.Id, kind = m.Kind.ToString(), m.Version, m.ArtifactRef,
                providesClasses = m.ProvidesClasses is null ? null : (JsonElement?)JsonDocument.Parse(m.ProvidesClasses).RootElement,
            }));
        });

        models.MapPost("/", async (CreateVisionModelRequest req, IMesConfigDbContext db, CancellationToken ct) =>
        {
            if (!Enum.TryParse<VisionModelKind>(req.Kind.Replace("_", string.Empty), true, out var kind))
            {
                return Results.BadRequest(new { error = $"kind inválido: '{req.Kind}' (object_detection|action_recognition|pose)" });
            }

            var model = new VisionModel(Guid.NewGuid(), kind, req.Version, req.ArtifactRef)
            {
                ProvidesClasses = req.ProvidesClasses?.GetRawText(),
            };
            db.VisionModels.Add(model);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/v1/vision-models/{model.Id}", new { model.Id });
        });

        return app;
    }
}

public sealed record CreateDetectionClassRequest(string Kind, string Code, string Name, string Scope);

public sealed record CreateVisionModelRequest(string Kind, string Version, string ArtifactRef, JsonElement? ProvidesClasses = null);
