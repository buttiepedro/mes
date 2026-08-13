using System.Collections.Concurrent;
using System.Text.Json;

namespace Nexo.RulesEngine;

public enum ObsKind { Vision, Signal }

/// <summary>Observación normalizada: detección de visión o lectura de señal.</summary>
public sealed record Observation(
    ObsKind Type,
    string? CameraId, string[] Zones, string? VisionKind, string? Class, double Score, string? TrackId,
    string? SignalId, JsonElement? Value,
    DateTimeOffset At);

public static class ObservationParser
{
    public static Observation Parse(JsonElement e)
    {
        var at = e.TryGetProperty("at", out var a) && a.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(a.GetString(), out var t)
            ? t : DateTimeOffset.UtcNow;

        if (Str(e, "obs_type") == "signal")
        {
            return new Observation(ObsKind.Signal, null, Array.Empty<string>(), null, null, 0, null,
                Str(e, "signal_id"), e.TryGetProperty("value", out var v) ? v.Clone() : null, at);
        }

        var zones = e.TryGetProperty("zones", out var z) && z.ValueKind == JsonValueKind.Array
            ? z.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!).ToArray()
            : Array.Empty<string>();

        return new Observation(ObsKind.Vision, Str(e, "camera_id"), zones, Str(e, "kind"), Str(e, "class"),
            e.TryGetProperty("score", out var s) && s.TryGetDouble(out var sc) ? sc : 1.0, Str(e, "track_id"),
            null, null, at);
    }

    private static string? Str(JsonElement e, string p)
        => e.TryGetProperty(p, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}

/// <summary>Estado del mundo: detecciones recientes (con ventana de presencia) + última lectura por señal.</summary>
public sealed class WorldState
{
    private readonly List<Observation> _detections = new();
    private readonly Dictionary<string, Observation> _signals = new();

    public TimeSpan PresenceWindow { get; set; } = TimeSpan.FromSeconds(1);

    public void Apply(Observation o)
    {
        if (o.Type == ObsKind.Vision) _detections.Add(o);
        else if (o.SignalId is not null) _signals[o.SignalId] = o;
    }

    public void Prune(DateTimeOffset now) => _detections.RemoveAll(d => now - d.At > PresenceWindow);

    public (List<Observation> Detections, Dictionary<string, Observation> Signals) Snapshot()
        => (_detections.ToList(), new Dictionary<string, Observation>(_signals));
}

/// <summary>Evalúa un nodo del disparador (gramática de docs/design/rules-and-events.md) sobre el world-state.</summary>
public static class Evaluator
{
    public static bool EvalBool(JsonElement n, List<Observation> dets, Dictionary<string, Observation> sigs, out Observation? matched)
    {
        matched = null;
        switch (n.GetProperty("op").GetString())
        {
            case "match":
                return EvalMatch(n, dets, out matched);
            case "signal":
                return EvalSignal(n, sigs);
            case "and":
                foreach (var c in n.GetProperty("of").EnumerateArray())
                {
                    if (!EvalBool(c, dets, sigs, out var m)) return false;
                    matched ??= m;
                }
                return true;
            case "or":
                foreach (var c in n.GetProperty("of").EnumerateArray())
                {
                    if (EvalBool(c, dets, sigs, out matched)) return true;
                }
                return false;
            case "not":
                return !EvalBool(n.GetProperty("of"), dets, sigs, out _);
            case "sustained":
                return EvalBool(n.GetProperty("of"), dets, sigs, out matched); // duración se maneja a nivel regla
            default:
                return false;
        }
    }

    public static bool MatchesObs(JsonElement matchNode, Observation o)
        => matchNode.GetProperty("op").GetString() == "match"
            ? EvalMatch(matchNode, new List<Observation> { o }, out _)
            : EvalBool(matchNode, new List<Observation> { o }, new Dictionary<string, Observation>(), out _);

    private static bool EvalMatch(JsonElement n, List<Observation> dets, out Observation? matched)
    {
        matched = null;
        var src = n.TryGetProperty("source", out var s) ? s : default;
        string? cam = src.ValueKind == JsonValueKind.Object && src.TryGetProperty("camera_id", out var c) ? c.GetString() : null;
        string? zone = src.ValueKind == JsonValueKind.Object && src.TryGetProperty("zone_id", out var z) ? z.GetString() : null;
        string? kind = n.TryGetProperty("kind", out var k) ? k.GetString() : null;
        string? cls = n.TryGetProperty("class", out var cl) ? cl.GetString() : null;
        double minScore = n.TryGetProperty("where", out var w) && w.TryGetProperty("score_gte", out var sg) && sg.TryGetDouble(out var mg) ? mg : 0;

        foreach (var d in dets)
        {
            if (cam is not null && d.CameraId != cam) continue;
            if (zone is not null && !d.Zones.Contains(zone)) continue;
            if (kind is not null && !string.Equals(d.VisionKind, kind, StringComparison.OrdinalIgnoreCase)) continue;
            if (cls is not null && !string.Equals(d.Class, cls, StringComparison.OrdinalIgnoreCase)) continue;
            if (d.Score < minScore) continue;
            matched = d;
            return true;
        }
        return false;
    }

    private static bool EvalSignal(JsonElement n, Dictionary<string, Observation> sigs)
    {
        var sid = n.GetProperty("signal_id").GetString();
        if (sid is null || !sigs.TryGetValue(sid, out var o) || o.Value is not { } val) return false;
        return Compare(val, n.TryGetProperty("cmp", out var cEl) ? cEl.GetString() : "==", n.GetProperty("value"));
    }

    private static bool Compare(JsonElement a, string? cmp, JsonElement b)
    {
        cmp ??= "==";
        if (a.ValueKind == JsonValueKind.Number && b.ValueKind == JsonValueKind.Number)
        {
            double x = a.GetDouble(), y = b.GetDouble();
            return cmp switch { ">" => x > y, ">=" => x >= y, "<" => x < y, "<=" => x <= y, "!=" => Math.Abs(x - y) > 1e-9, _ => Math.Abs(x - y) <= 1e-9 };
        }

        string sa = a.ValueKind == JsonValueKind.String ? a.GetString()! : a.GetRawText().Trim('"');
        string sb = b.ValueKind == JsonValueKind.String ? b.GetString()! : b.GetRawText().Trim('"');
        return cmp == "!=" ? !string.Equals(sa, sb, StringComparison.OrdinalIgnoreCase) : string.Equals(sa, sb, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>Estado de ejecución de una regla (edge-trigger + timers de sustained + ventana de count).</summary>
public sealed class RuleRuntime
{
    public required string Code { get; init; }
    public required JsonElement Trigger { get; init; }
    public required JsonElement Emit { get; init; }
    public int CooldownSeconds { get; init; }

    public string TopOp => Trigger.GetProperty("op").GetString() ?? string.Empty;
    public DateTimeOffset? LastFired;
    public bool LastBool;
    public DateTimeOffset? SustainedSince;
    public bool SustainedFired;
    public readonly List<DateTimeOffset> CountHits = new();
    public int SeqIndex;
    public DateTimeOffset? SeqPrevAt;
}
