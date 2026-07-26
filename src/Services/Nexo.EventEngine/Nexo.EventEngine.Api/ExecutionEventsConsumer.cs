using System.Text.Json;
using Confluent.Kafka;

namespace Nexo.EventEngine.Api;

/// <summary>
/// Kafka consumer that feeds the <see cref="ExecutionProgressProjection"/>. It subscribes (by regex) to
/// every <c>nexo.execution.*</c> / <c>nexo.task.*</c> topic and replays them into the in-memory read model.
/// </summary>
/// <remarks>
/// Reads from the <b>earliest</b> offset and never commits, so on each start the projection is rebuilt
/// from the full retained log — the natural fit for the volatile in-memory read model (M2). A persistent
/// read model with committed offsets is deferred.
/// </remarks>
public sealed class ExecutionEventsConsumer : BackgroundService
{
    // librdkafka treats a subscription starting with '^' as a regex over topic names.
    private const string TopicPattern = "^nexo\\.(execution|task)\\..*";

    private readonly ExecutionProgressProjection _projection;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ExecutionEventsConsumer> _logger;

    public ExecutionEventsConsumer(
        ExecutionProgressProjection projection,
        IConfiguration configuration,
        ILogger<ExecutionEventsConsumer> logger)
    {
        _projection = projection;
        _configuration = configuration;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        // Confluent's Consume is blocking; run the loop off the startup thread.
        => Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);

    private void ConsumeLoop(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = "nexo-event-engine",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            AllowAutoCreateTopics = false,
            // Regex subscriptions only pick up newly-created topics on a metadata refresh; the default
            // interval is 5 min, far too slow for an engine that should react to a new execution promptly.
            TopicMetadataRefreshIntervalMs = 5000,
        };

        using var consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, error) => _logger.LogWarning("Kafka error: {Reason}", error.Reason))
            .Build();

        consumer.Subscribe(TopicPattern);
        _logger.LogInformation("Event engine consumer subscribed to {Pattern}", TopicPattern);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(TimeSpan.FromMilliseconds(500));
                    if (result?.Message?.Value is null)
                    {
                        continue;
                    }

                    using var document = JsonDocument.Parse(result.Message.Value);
                    var root = document.RootElement;

                    var type = root.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null;
                    if (!string.IsNullOrEmpty(type))
                    {
                        _projection.Apply(type, root);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogWarning(ex, "Consume error");
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Skipping malformed event payload");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // graceful shutdown
        }
        finally
        {
            consumer.Close();
        }
    }
}
