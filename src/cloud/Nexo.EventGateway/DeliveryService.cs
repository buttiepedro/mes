using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Nexo.EventGateway;

/// <summary>
/// Entrega los Eventos canónicos a HEXA: firma HMAC-SHA256 por empresa (header <c>X-MES-Signature</c>),
/// reintentos con backoff, y un log de entregas para observabilidad. (Idempotencia por <c>dedup_key</c>
/// del lado de HEXA.) Ver [HEXA-INTEGRATION.md] §4.3.
/// </summary>
public sealed class DeliveryService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<DeliveryService> _log;
    private readonly ConcurrentQueue<object> _deliveries = new();

    public DeliveryService(IHttpClientFactory httpFactory, IConfiguration config, ILogger<DeliveryService> log)
    {
        _httpFactory = httpFactory;
        _config = config;
        _log = log;
    }

    public IReadOnlyList<object> Deliveries => _deliveries.ToList();

    public async Task DeliverAsync(JsonElement evt, CancellationToken ct)
    {
        var url = _config["Hexa:WebhookUrl"];
        var secret = _config["Hexa:WebhookSecret"] ?? string.Empty;
        var body = evt.GetRawText();
        var eventType = evt.TryGetProperty("event_type", out var t) ? t.GetString() : null;
        var dedup = evt.TryGetProperty("dedup_key", out var d) ? d.GetString() : null;

        if (string.IsNullOrEmpty(url))
        {
            Record(eventType, dedup, "sin Hexa:WebhookUrl");
            return;
        }

        var signature = "sha256=" + Hmac(secret, body);
        var client = _httpFactory.CreateClient();

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json"),
                };
                req.Headers.TryAddWithoutValidation("X-MES-Signature", signature);

                using var resp = await client.SendAsync(req, ct);
                if (resp.IsSuccessStatusCode)
                {
                    Record(eventType, dedup, $"entregado ({(int)resp.StatusCode}) en intento {attempt}");
                    return;
                }

                if (attempt == 3) Record(eventType, dedup, $"fallo HTTP {(int)resp.StatusCode}");
            }
            catch (Exception ex)
            {
                if (attempt == 3) Record(eventType, dedup, "error: " + ex.Message);
            }

            try { await Task.Delay(attempt * 300, ct); }
            catch (OperationCanceledException) { break; }
        }
    }

    private void Record(string? eventType, string? dedup, string status)
    {
        _deliveries.Enqueue(new { at = DateTimeOffset.UtcNow, eventType, dedupKey = dedup, status });
        _log.LogInformation("Entrega a HEXA: {Type} → {Status}", eventType, status);
    }

    private static string Hmac(string secret, string body)
    {
        using var h = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(h.ComputeHash(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
    }
}
