using System.Collections.Concurrent;
using System.Text.Json;

namespace Nexo.RulesEngine;

/// <summary>
/// El motor de eventos (Capa 4): mantiene el world-state, evalúa las reglas contra el stream de
/// observaciones y emite Eventos canónicos. Slice 1: reglas y observaciones se cargan por HTTP; la carga
/// desde la config (config-bundle) y la salida a HEXA (event-gateway) son los siguientes pasos.
/// </summary>
public sealed class RulesEngineService : BackgroundService
{
    private readonly WorldState _world = new();
    private readonly List<RuleRuntime> _rules = new();
    private readonly ConcurrentQueue<JsonElement> _events = new();
    private readonly object _lock = new();
    private readonly ILogger<RulesEngineService> _log;
    private readonly IHttpClientFactory _httpFactory;
    private readonly string? _sinkUrl;

    public RulesEngineService(ILogger<RulesEngineService> log, IHttpClientFactory httpFactory, IConfiguration config)
    {
        _log = log;
        _httpFactory = httpFactory;
        _sinkUrl = config["Events:SinkUrl"]; // event-gateway (o directo HEXA en su defecto)
    }

    public void LoadRules(IEnumerable<RuleRuntime> rules)
    {
        lock (_lock)
        {
            _rules.Clear();
            _rules.AddRange(rules);
        }
    }

    public int RuleCount { get { lock (_lock) { return _rules.Count; } } }

    public IReadOnlyList<JsonElement> Events => _events.ToList();

    public void Ingest(Observation o)
    {
        lock (_lock)
        {
            _world.Apply(o);
            _world.Prune(o.At);
            EvaluateAll(o.At, o);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Tick: reevalúa sustained/count aunque no lleguen observaciones nuevas.
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await Task.Delay(500, stoppingToken); }
            catch (OperationCanceledException) { break; }

            lock (_lock)
            {
                var now = DateTimeOffset.UtcNow;
                _world.Prune(now);
                EvaluateAll(now, null);
            }
        }
    }

    private void EvaluateAll(DateTimeOffset now, Observation? obs)
    {
        var (dets, sigs) = _world.Snapshot();

        foreach (var r in _rules)
        {
            switch (r.TopOp)
            {
                case "sustained":
                {
                    var forSeconds = r.Trigger.TryGetProperty("for_seconds", out var f) ? f.GetDouble() : 0;
                    var value = Evaluator.EvalBool(r.Trigger.GetProperty("of"), dets, sigs, out var matched);
                    if (value)
                    {
                        r.SustainedSince ??= now;
                        var elapsed = (now - r.SustainedSince.Value).TotalSeconds;
                        if (!r.SustainedFired && elapsed >= forSeconds)
                        {
                            Fire(r, now, matched, durationSeconds: elapsed);
                            r.SustainedFired = true;
                        }
                    }
                    else
                    {
                        r.SustainedSince = null;
                        r.SustainedFired = false;
                    }
                    break;
                }

                case "count":
                {
                    var n = r.Trigger.TryGetProperty("n", out var ne) ? ne.GetInt32() : 1;
                    var window = r.Trigger.TryGetProperty("window_seconds", out var we) ? we.GetDouble() : 60;
                    if (obs is not null && Evaluator.MatchesObs(r.Trigger.GetProperty("of"), obs)) r.CountHits.Add(now);
                    r.CountHits.RemoveAll(t => (now - t).TotalSeconds > window);
                    if (r.CountHits.Count >= n)
                    {
                        Fire(r, now, obs, count: r.CountHits.Count);
                        r.CountHits.Clear();
                    }
                    break;
                }

                case "sequence":
                {
                    var within = r.Trigger.TryGetProperty("within_seconds", out var wEl) ? wEl.GetDouble() : 0;
                    var steps = r.Trigger.GetProperty("steps");
                    var stepCount = steps.GetArrayLength();

                    // Timeout: si tardó más que la ventana desde el paso anterior, reinicia la secuencia.
                    if (r.SeqIndex > 0 && r.SeqPrevAt is { } prev && (now - prev).TotalSeconds > within)
                    {
                        r.SeqIndex = 0;
                        r.SeqPrevAt = null;
                    }

                    if (Evaluator.EvalBool(steps[r.SeqIndex], dets, sigs, out var seqMatched))
                    {
                        r.SeqIndex++;
                        r.SeqPrevAt = now;
                        if (r.SeqIndex >= stepCount)
                        {
                            Fire(r, now, seqMatched);
                            r.SeqIndex = 0;
                            r.SeqPrevAt = null;
                        }
                    }
                    break;
                }

                default: // match / signal / and / or / not — edge trigger (dispara al pasar de false a true)
                {
                    var value = Evaluator.EvalBool(r.Trigger, dets, sigs, out var matched);
                    if (value && !r.LastBool) Fire(r, now, matched);
                    r.LastBool = value;
                    break;
                }
            }
        }
    }

    private void Fire(RuleRuntime r, DateTimeOffset now, Observation? matched, double? durationSeconds = null, int? count = null)
    {
        if (r.LastFired is { } last && (now - last).TotalSeconds < r.CooldownSeconds) return; // cooldown
        r.LastFired = now;

        var evt = BuildEvent(r, now, matched, durationSeconds, count);
        _events.Enqueue(evt);

        var type = r.Emit.TryGetProperty("event_type", out var et) ? et.GetString() : "?";
        _log.LogInformation("Evento emitido: {Type} (regla {Rule})", type, r.Code);

        // Reenvío al sink (event-gateway → HEXA). Fire-and-forget; el gateway maneja firma/reintentos.
        if (!string.IsNullOrEmpty(_sinkUrl))
        {
            var body = evt.GetRawText();
            _ = Task.Run(async () =>
            {
                try
                {
                    using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
                    await _httpFactory.CreateClient().PostAsync(_sinkUrl, content);
                }
                catch (Exception ex) { _log.LogWarning(ex, "No se pudo reenviar el evento al sink {Sink}", _sinkUrl); }
            });
        }
    }

    private static JsonElement BuildEvent(RuleRuntime r, DateTimeOffset now, Observation? m, double? duration, int? count)
    {
        var values = new Dictionary<string, object?>();
        if (r.Emit.TryGetProperty("payload", out var payload) && payload.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in payload.EnumerateObject())
            {
                values[prop.Name] = Substitute(prop.Value, m, duration, count);
            }
        }

        var source = new Dictionary<string, object?>();
        if (m is { Type: ObsKind.Vision })
        {
            source["camera_id"] = m.CameraId;
            source["zone_id"] = m.Zones.FirstOrDefault();
        }
        else if (m is { Type: ObsKind.Signal })
        {
            source["signal_id"] = m.SignalId;
        }

        var evt = new Dictionary<string, object?>
        {
            ["event_id"] = Guid.NewGuid().ToString(),
            ["dedup_key"] = $"{r.Code}:{m?.TrackId ?? m?.SignalId ?? "-"}",
            ["event_type"] = r.Emit.TryGetProperty("event_type", out var et) ? et.GetString() : "event",
            ["severity"] = r.Emit.TryGetProperty("severity", out var sv) ? sv.GetString() : "info",
            ["rule_code"] = r.Code,
            ["occurred_at"] = now,
            ["source"] = source,
            ["values"] = values,
        };

        if (r.Emit.TryGetProperty("evidence", out var evidence))
        {
            evt["evidence"] = JsonSerializer.Deserialize<JsonElement>(evidence.GetRawText());
        }

        return JsonSerializer.SerializeToElement(evt);
    }

    private static object? Substitute(JsonElement v, Observation? m, double? duration, int? count)
    {
        if (v.ValueKind != JsonValueKind.String)
        {
            return JsonSerializer.Deserialize<JsonElement>(v.GetRawText());
        }

        return (v.GetString() ?? string.Empty)
            .Replace("{{obs.class}}", m?.Class ?? string.Empty)
            .Replace("{{obs.score}}", m?.Score.ToString() ?? string.Empty)
            .Replace("{{obs.track_id}}", m?.TrackId ?? string.Empty)
            .Replace("{{count}}", count?.ToString() ?? string.Empty)
            .Replace("{{duration_seconds}}", duration?.ToString("0.#") ?? string.Empty);
    }
}
