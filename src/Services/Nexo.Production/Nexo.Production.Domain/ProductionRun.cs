using Nexo.BuildingBlocks.Domain;

namespace Nexo.Production.Domain;

/// <summary>
/// A production run on a machine/shift. Aggregate root that owns its <see cref="ProductionRecord"/>s
/// and emits domain events on registration and closure.
/// </summary>
public sealed class ProductionRun : AggregateRoot<Guid>
{
    private readonly List<ProductionRecord> _records = new();

    // EF Core materialization constructor.
    private ProductionRun()
    {
    }

    private ProductionRun(Guid id, Guid workOrderId, Guid machineId, Guid shiftId, DateTimeOffset startedAt)
    {
        Id = id;
        WorkOrderId = workOrderId;
        MachineId = machineId;
        ShiftId = shiftId;
        StartedAt = startedAt;
        Status = RunStatus.Open;
    }

    public Guid WorkOrderId { get; private set; }

    public Guid MachineId { get; private set; }

    public Guid ShiftId { get; private set; }

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset? ClosedAt { get; private set; }

    public RunStatus Status { get; private set; }

    public IReadOnlyCollection<ProductionRecord> Records => _records.AsReadOnly();

    public static ProductionRun Open(Guid workOrderId, Guid machineId, Guid shiftId)
        => new(UuidV7.NewGuid(), workOrderId, machineId, shiftId, DateTimeOffset.UtcNow);

    /// <summary>
    /// Registers a production entry against this run, appending a <see cref="ProductionRecord"/>
    /// and raising a <see cref="ProductionRegisteredDomainEvent"/>.
    /// </summary>
    public ProductionRecord Register(Quantity good, Quantity scrap, Guid operatorId, ProductionSource source)
    {
        if (Status != RunStatus.Open)
        {
            throw new InvalidOperationException("Cannot register production on a run that is not open.");
        }

        var record = new ProductionRecord(
            UuidV7.NewGuid(),
            Id,
            good,
            scrap,
            operatorId,
            DateTimeOffset.UtcNow,
            source);

        _records.Add(record);

        Raise(new ProductionRegisteredDomainEvent(Id, WorkOrderId, good.Value, scrap.Value));

        return record;
    }

    /// <summary>Closes the run and raises a <see cref="RunClosedDomainEvent"/>.</summary>
    public void Close()
    {
        if (Status == RunStatus.Closed)
        {
            throw new InvalidOperationException("Run is already closed.");
        }

        Status = RunStatus.Closed;
        ClosedAt = DateTimeOffset.UtcNow;

        Raise(new RunClosedDomainEvent(Id, WorkOrderId));
    }
}
