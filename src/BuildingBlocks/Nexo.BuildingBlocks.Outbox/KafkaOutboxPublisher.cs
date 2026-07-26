using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Nexo.BuildingBlocks.Messaging;

namespace Nexo.BuildingBlocks.Outbox;

/// <summary>
/// Publishes outbox rows to Kafka. The canonical event <see cref="OutboxMessage.Type"/> maps to the
/// topic <c>{type}.v1</c> (same convention as the MassTransit producers), the stored JSON
/// <see cref="OutboxMessage.Payload"/> is the message value, and the tenant is the partition key so a
/// tenant's events keep their order.
/// </summary>
public sealed class KafkaOutboxPublisher : IOutboxPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;

    public KafkaOutboxPublisher(IConfiguration configuration)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            Acks = Acks.All,
            AllowAutoCreateTopics = true,
            EnableIdempotence = true,
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var topic = TopicFor(message.Type);

        var kafkaMessage = new Message<string, string>
        {
            Key = message.TenantId.ToString(),
            Value = message.Payload,
            Headers = new Headers
            {
                { "nexo-event-type", Encoding.UTF8.GetBytes(message.Type) },
                { "nexo-event-id", Encoding.UTF8.GetBytes(message.Id.ToString()) },
                { "nexo-tenant-id", Encoding.UTF8.GetBytes(message.TenantId.ToString()) },
                { "nexo-occurred-on", Encoding.UTF8.GetBytes(message.OccurredOn.ToString("O")) },
            },
        };

        await _producer.ProduceAsync(topic, kafkaMessage, cancellationToken);
    }

    /// <summary>Canonical topic name for an event type: <c>{type}.v1</c>.</summary>
    public static string TopicFor(string eventType) => $"{eventType}.v1";

    public void Dispose()
    {
        // Flush any in-flight deliveries before the app exits so no acknowledged-but-unsent messages are lost.
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
