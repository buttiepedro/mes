namespace Nexo.BuildingBlocks.Messaging;

/// <summary>
/// Transactional outbox row. Integration events are persisted here in the same transaction
/// as the aggregate change and dispatched to the bus by a background processor.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public DateTimeOffset OccurredOn { get; set; }

    public DateTimeOffset? ProcessedOn { get; set; }

    public string? Error { get; set; }
}
