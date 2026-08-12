using System.Text.Json;

namespace Nexo.RulesEngine;

public static class RulesEngineEndpoints
{
    public static IEndpointRouteBuilder MapRulesEngineEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1").WithTags("Rules Engine").RequireAuthorization();

        // Carga el set de reglas a evaluar. Body: una regla o un array de { code, trigger, emit, cooldownSeconds, enabled }.
        group.MapPost("/rules:load", (JsonElement body, RulesEngineService engine) =>
        {
            var rules = new List<RuleRuntime>();
            foreach (var item in Items(body))
            {
                if (item.TryGetProperty("enabled", out var en) && en.ValueKind == JsonValueKind.False) continue;

                rules.Add(new RuleRuntime
                {
                    Code = item.TryGetProperty("code", out var c) ? c.GetString() ?? "?" : "?",
                    Trigger = item.GetProperty("trigger").Clone(),
                    Emit = item.GetProperty("emit").Clone(),
                    CooldownSeconds = item.TryGetProperty("cooldownSeconds", out var cd) && cd.TryGetInt32(out var n) ? n
                        : item.TryGetProperty("cooldown_seconds", out var cd2) && cd2.TryGetInt32(out var n2) ? n2 : 0,
                });
            }

            engine.LoadRules(rules);
            return Results.Ok(new { loaded = rules.Count });
        });

        // Ingiere observaciones (visión/señal). Body: una observación o un array.
        group.MapPost("/observations", (JsonElement body, RulesEngineService engine) =>
        {
            var count = 0;
            foreach (var item in Items(body))
            {
                engine.Ingest(ObservationParser.Parse(item));
                count++;
            }
            return Results.Ok(new { ingested = count, events = engine.Events.Count });
        });

        group.MapGet("/events", (RulesEngineService engine) => Results.Ok(engine.Events));

        group.MapGet("/rules", (RulesEngineService engine) => Results.Ok(new { count = engine.RuleCount }));

        return app;
    }

    private static IEnumerable<JsonElement> Items(JsonElement body)
        => body.ValueKind == JsonValueKind.Array ? body.EnumerateArray().ToList() : new List<JsonElement> { body };
}
