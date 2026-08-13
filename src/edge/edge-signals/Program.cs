using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;

// ── edge-signals ──────────────────────────────────────────────────────────────────────────────────
// Agente EDGE (por planta): lee señales industriales por MQTT y las manda como OBSERVACIONES a la nube.
// Hace pull de la config (qué señales/topics) desde el config-bundle de Nexo.MesApi. No manda a HEXA
// directo: manda observaciones a la nube (RulesEngine / ingesta). Buffer simple con reintento.

var broker = Env("BROKER_HOST", "localhost");
var brokerPort = int.TryParse(Env("BROKER_PORT", "1883"), out var bp) ? bp : 1883;
var bundleUrl = Env("BUNDLE_URL", "http://localhost:5085/v1/config-bundle");
var observationsUrl = Env("OBSERVATIONS_URL", "http://localhost:5086/v1/observations");

var http = new HttpClient();
var signals = new Dictionary<string, SignalDef>(); // mqttTopic -> señal

await LoadConfigAsync();

var client = new MqttFactory().CreateMqttClient();

client.ApplicationMessageReceivedAsync += async e =>
{
    var topic = e.ApplicationMessage.Topic;
    if (!signals.TryGetValue(topic, out var sig)) return;

    var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
    var raw = ExtractValue(payload, sig.JsonPath);
    var vtype = sig.ValueType.ToLowerInvariant(); // el config-bundle trae el enum en PascalCase (Number/Bool/String)
    object typed = vtype switch
    {
        "number" => double.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var num) ? num : raw,
        "bool" => bool.TryParse(raw, out var b) ? b : raw,
        _ => raw,
    };

    var observation = new
    {
        obs_type = "signal",
        signal_id = sig.Code,
        value = typed,
        vtype,
        at = DateTimeOffset.UtcNow,
    };

    try
    {
        var json = JsonSerializer.Serialize(observation);
        await http.PostAsync(observationsUrl, new StringContent(json, Encoding.UTF8, "application/json"));
        Console.WriteLine($"[edge-signals] {topic} → {sig.Code}={typed}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[edge-signals] POST observación falló ({ex.Message}) — TODO: buffer store-and-forward");
    }
};

var options = new MqttClientOptionsBuilder()
    .WithTcpServer(broker, brokerPort)
    .WithClientId("nexo-edge-signals")
    .Build();

for (var attempt = 1; ; attempt++)
{
    try { await client.ConnectAsync(options, CancellationToken.None); break; }
    catch (Exception ex)
    {
        Console.WriteLine($"[edge-signals] conexión MQTT falló (intento {attempt}): {ex.Message}");
        if (attempt >= 10) return;
        await Task.Delay(2000);
    }
}

Console.WriteLine($"[edge-signals] conectado a MQTT {broker}:{brokerPort}");
foreach (var topic in signals.Keys)
{
    await client.SubscribeAsync(topic);
    Console.WriteLine($"[edge-signals] suscrito a {topic}");
}

// Mantener vivo (y refrescar config cada 30 s para tomar señales nuevas).
while (true)
{
    await Task.Delay(TimeSpan.FromSeconds(30));
    var before = signals.Keys.ToHashSet();
    await LoadConfigAsync();
    foreach (var topic in signals.Keys.Except(before))
    {
        await client.SubscribeAsync(topic);
        Console.WriteLine($"[edge-signals] suscrito (nuevo) a {topic}");
    }
}

async Task LoadConfigAsync()
{
    try
    {
        using var doc = JsonDocument.Parse(await http.GetStringAsync(bundleUrl));
        if (!doc.RootElement.TryGetProperty("signals", out var sigs) || sigs.ValueKind != JsonValueKind.Array) return;

        signals.Clear();
        foreach (var s in sigs.EnumerateArray())
        {
            var topic = s.TryGetProperty("mqttTopic", out var mt) ? mt.GetString() : null;
            if (string.IsNullOrEmpty(topic)) continue;
            signals[topic] = new SignalDef(
                s.TryGetProperty("code", out var c) ? c.GetString() ?? topic : topic,
                s.TryGetProperty("jsonPath", out var jp) && jp.ValueKind == JsonValueKind.String ? jp.GetString() : null,
                s.TryGetProperty("valueType", out var vt) ? vt.GetString() ?? "string" : "string");
        }
        Console.WriteLine($"[edge-signals] {signals.Count} señal(es) cargada(s) de la config");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[edge-signals] no se pudo cargar la config desde {bundleUrl}: {ex.Message}");
    }
}

static string ExtractValue(string payload, string? jsonPath)
{
    if (string.IsNullOrEmpty(jsonPath)) return payload.Trim();
    try
    {
        using var doc = JsonDocument.Parse(payload);
        var el = doc.RootElement;
        foreach (var part in jsonPath.TrimStart('$', '.').Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            el = el.GetProperty(part);
        }
        return el.ToString();
    }
    catch { return payload.Trim(); }
}

static string Env(string name, string fallback) => Environment.GetEnvironmentVariable(name) is { Length: > 0 } v ? v : fallback;

internal sealed record SignalDef(string Code, string? JsonPath, string ValueType);
