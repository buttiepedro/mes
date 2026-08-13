using System.Text.Json;

namespace Nexo.RulesEngine;

/// <summary>
/// Carga las reglas desde la configuración (hace *pull* del <c>config-bundle</c> de <c>Nexo.MesApi</c>)
/// y las mantiene sincronizadas. Reemplaza la carga manual por HTTP como fuente real de reglas.
/// Solo recarga cuando el set de reglas cambió (evita resetear el estado del motor cada ciclo).
/// </summary>
public sealed class ConfigLoader : BackgroundService
{
    private readonly RulesEngineService _engine;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<ConfigLoader> _log;
    private string? _lastHash;

    public ConfigLoader(RulesEngineService engine, IHttpClientFactory httpFactory, IConfiguration config, ILogger<ConfigLoader> log)
    {
        _engine = engine;
        _httpFactory = httpFactory;
        _config = config;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var url = _config["Config:BundleUrl"];
        if (string.IsNullOrWhiteSpace(url))
        {
            _log.LogInformation("Config:BundleUrl no configurado; las reglas se cargan por HTTP (POST /v1/rules:load).");
            return;
        }

        var client = _httpFactory.CreateClient();
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await LoadOnceAsync(client, url, stoppingToken); }
            catch (Exception ex) { _log.LogWarning(ex, "No se pudo cargar la config desde {Url}", url); }

            try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task LoadOnceAsync(HttpClient client, string url, CancellationToken ct)
    {
        // En prod la llamada lleva un service-token de HEXA/plataforma; en dev el dev-bypass la acepta.
        using var response = await client.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        if (!doc.RootElement.TryGetProperty("rules", out var rulesEl) || rulesEl.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var hash = rulesEl.GetRawText();
        if (hash == _lastHash)
        {
            return; // sin cambios
        }

        var rules = new List<RuleRuntime>();
        foreach (var r in rulesEl.EnumerateArray())
        {
            if (r.TryGetProperty("enabled", out var en) && en.ValueKind == JsonValueKind.False) continue;
            if (!r.TryGetProperty("trigger", out var trigger) || !r.TryGetProperty("emit", out var emit)) continue;

            rules.Add(new RuleRuntime
            {
                Code = r.TryGetProperty("code", out var c) ? c.GetString() ?? "?" : "?",
                Trigger = trigger.Clone(),
                Emit = emit.Clone(),
                CooldownSeconds = r.TryGetProperty("cooldownSeconds", out var cd) && cd.TryGetInt32(out var n) ? n : 0,
            });
        }

        _engine.LoadRules(rules);
        _lastHash = hash;
        _log.LogInformation("Reglas cargadas desde la config: {Count}", rules.Count);
    }
}
