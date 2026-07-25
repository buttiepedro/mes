using Nexo.BuildingBlocks.Domain;

namespace Nexo.Execution.Domain;

/// <summary>
/// A real input consumption (<c>execution.input_consumptions</c>, §2.7.3). Child entity of the
/// <see cref="Execution"/> aggregate.
/// </summary>
/// <remarks>
/// <b>No cost (MOD-17).</b> Consumption is quantity, unit and (optionally) batch — never valuation, rate
/// or cost; that is deferred to V1. <see cref="ItemId"/>/<see cref="UomId"/> are logical references to
/// <c>master.*</c> and <see cref="BatchId"/> to <c>trace.batches</c>: uuid <b>without a physical foreign
/// key</b> (§1.9). The deviation against the standard is not stored: it is derived by Layer 4.
/// </remarks>
public sealed class InputConsumption : Entity<Guid>
{
    // EF Core materialization constructor.
    private InputConsumption()
    {
    }

    private InputConsumption(
        Guid id,
        Guid executionId,
        Guid? taskRunId,
        Guid? taskInputId,
        Guid itemId,
        decimal quantity,
        Guid uomId,
        decimal? plannedQuantity,
        ConsumptionMethod method,
        Guid? batchId,
        Guid? serialId,
        Guid? personId)
        : base(id)
    {
        ExecutionId = executionId;
        TaskRunId = taskRunId;
        TaskInputId = taskInputId;
        ItemId = itemId;
        Quantity = quantity;
        UomId = uomId;
        PlannedQuantity = plannedQuantity;
        Method = method;
        BatchId = batchId;
        SerialId = serialId;
        PersonId = personId;
        RecordedAt = DateTimeOffset.UtcNow;
        CreatedAt = RecordedAt;
        UpdatedAt = RecordedAt;
    }

    public Guid ExecutionId { get; private set; }

    /// <summary>Fine attribution to a task run; <c>null</c> only for execution-level consumption.</summary>
    public Guid? TaskRunId { get; private set; }

    /// <summary>The Layer 2 standard this consumption refers to (<c>work.task_inputs</c>); <c>null</c> if ad-hoc.</summary>
    public Guid? TaskInputId { get; private set; }

    /// <summary>Logical reference to <c>master.items</c> — no physical foreign key.</summary>
    public Guid ItemId { get; private set; }

    /// <summary>Real consumed quantity. Never zero and, in this slice, never negative.</summary>
    public decimal Quantity { get; private set; }

    /// <summary>Logical reference to <c>master.uom</c> — no physical foreign key.</summary>
    public Guid UomId { get; private set; }

    /// <summary>Planned quantity (standard × target) at scheduling time, if known.</summary>
    public decimal? PlannedQuantity { get; private set; }

    public ConsumptionMethod Method { get; private set; }

    /// <summary>Consumed batch → genealogy (E15). Logical reference to <c>trace.batches</c>.</summary>
    public Guid? BatchId { get; private set; }

    public Guid? SerialId { get; private set; }

    public Guid? PersonId { get; private set; }

    public DateTimeOffset RecordedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public Guid? DeletedBy { get; private set; }

    internal static InputConsumption Create(
        Guid executionId,
        Guid? taskRunId,
        Guid? taskInputId,
        Guid itemId,
        decimal quantity,
        Guid uomId,
        decimal? plannedQuantity,
        ConsumptionMethod method,
        Guid? batchId,
        Guid? serialId,
        Guid? personId)
        => new(
            UuidV7.NewGuid(),
            executionId,
            taskRunId,
            taskInputId,
            itemId,
            quantity,
            uomId,
            plannedQuantity,
            method,
            batchId,
            serialId,
            personId);
}
