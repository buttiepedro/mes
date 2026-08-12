using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nexo.MesApi.Application;
using Nexo.MesApi.Domain;

namespace Nexo.MesApi.Api;

/// <summary>ABM de reglas. <c>trigger</c> y <c>emit</c> son JSON (gramática en docs/design/rules-and-events.md).</summary>
public static class RuleEndpoints
{
    public static IEndpointRouteBuilder MapRuleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/rules").WithTags("Config · Rules").RequireAuthorization();

        group.MapGet("/", async (IMesConfigDbContext db, CancellationToken ct) =>
        {
            var rules = await db.Rules.OrderBy(r => r.Code).ToListAsync(ct);
            return Results.Ok(rules.Select(r => new
            {
                r.Id,
                r.Code,
                r.Name,
                r.Enabled,
                r.ScopeLocationNodeId,
                r.CooldownSeconds,
                trigger = JsonDocument.Parse(r.Trigger).RootElement,
                emit = JsonDocument.Parse(r.Emit).RootElement,
            }));
        });

        group.MapPost("/", async (CreateRuleRequest req, IMesConfigDbContext db, CancellationToken ct) =>
        {
            var rule = new Rule(Guid.NewGuid(), req.Code, req.Name, req.Trigger.GetRawText(), req.Emit.GetRawText())
            {
                Enabled = req.Enabled,
                ScopeLocationNodeId = req.ScopeLocationNodeId,
                CooldownSeconds = req.CooldownSeconds,
            };
            db.Rules.Add(rule);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/v1/rules/{rule.Id}", new { rule.Id });
        });

        return app;
    }
}

/// <summary><c>Trigger</c> y <c>Emit</c> llegan como JSON (objeto), se guardan como texto jsonb.</summary>
public sealed record CreateRuleRequest(
    string Code,
    string Name,
    JsonElement Trigger,
    JsonElement Emit,
    bool Enabled = true,
    Guid? ScopeLocationNodeId = null,
    int CooldownSeconds = 0);
