using Nexo.BuildingBlocks.Domain;

namespace Nexo.Production.Domain;

/// <summary>
/// A single production entry (manual or datalogger) belonging to a <see cref="ProductionRun"/>.
/// </summary>
public sealed class ProductionRecord : Entity<Guid>
{
    // EF Core materialization constructor.
    private ProductionRecord()
    {
        GoodQty = Quantity.Zero;
        ScrapQty = Quantity.Zero;
    }

    internal ProductionRecord(
        Guid id,
        Guid runId,
        Quantity goodQty,
        Quantity scrapQty,
        Guid operatorId,
        DateTimeOffset recordedAt,
        ProductionSource source)
    {
        Id = id;
        RunId = runId;
        GoodQty = goodQty;
        ScrapQty = scrapQty;
        OperatorId = operatorId;
        RecordedAt = recordedAt;
        Source = source;
    }

    public Guid RunId { get; private set; }

    public Quantity GoodQty { get; private set; }

    public Quantity ScrapQty { get; private set; }

    public Guid OperatorId { get; private set; }

    public DateTimeOffset RecordedAt { get; private set; }

    public ProductionSource Source { get; private set; }
}
