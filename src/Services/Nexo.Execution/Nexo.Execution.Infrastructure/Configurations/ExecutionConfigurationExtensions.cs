using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Nexo.Execution.Application;
using Nexo.Execution.Domain;

namespace Nexo.Execution.Infrastructure.Configurations;

/// <summary>
/// Shared mapping pieces of the <c>execution</c> schema: the "live rows" filter used by every partial
/// index over a natural key and the value converters that persist the domain enums as the lower-case,
/// snake_case text the design's CHECK constraints spell (docs/design/03-data-schema.md §2.7 / §2.8).
/// </summary>
/// <remarks>
/// The converters delegate to <see cref="ExecutionWireValues"/> so the storage value and the wire value
/// are the same string by construction and can never drift apart — the same criterion Work Model / Master
/// Data use for their schemas.
/// </remarks>
internal static class ExecutionConfigurationExtensions
{
    /// <summary>Filter of every partial index over a natural key / soft-deletable row: only live rows compete.</summary>
    public const string LiveRowsFilter = "deleted_at IS NULL";

    public static readonly ValueConverter<ExecutionFlavor, string> FlavorConverter = new(
        flavor => flavor.ToWireValue(),
        value => Parse<ExecutionFlavor>(value));

    public static readonly ValueConverter<ExecutionStatus, string> ExecutionStatusConverter = new(
        status => status.ToWireValue(),
        value => Parse<ExecutionStatus>(value));

    public static readonly ValueConverter<TriggerKind, string> TriggerKindConverter = new(
        kind => kind.ToWireValue(),
        value => Parse<TriggerKind>(value));

    public static readonly ValueConverter<CloseKind, string> CloseKindConverter = new(
        kind => kind.ToWireValue(),
        value => Parse<CloseKind>(value));

    public static readonly ValueConverter<TaskRunStatus, string> TaskRunStatusConverter = new(
        status => status.ToWireValue(),
        value => Parse<TaskRunStatus>(value));

    public static readonly ValueConverter<AssignmentMode, string> AssignmentModeConverter = new(
        mode => mode.ToWireValue(),
        value => Parse<AssignmentMode>(value));

    public static readonly ValueConverter<ProgressMethod, string> ProgressMethodConverter = new(
        method => method.ToWireValue(),
        value => Parse<ProgressMethod>(value));

    public static readonly ValueConverter<TaskObligation, string> ObligationConverter = new(
        obligation => obligation.ToWireValue(),
        value => Parse<TaskObligation>(value));

    public static readonly ValueConverter<EvidenceKind, string> EvidenceKindConverter = new(
        kind => kind.ToWireValue(),
        value => Parse<EvidenceKind>(value));

    public static readonly ValueConverter<EvidenceStatus, string> EvidenceStatusConverter = new(
        status => status.ToWireValue(),
        value => Parse<EvidenceStatus>(value));

    public static readonly ValueConverter<DependencyType, string> DependencyTypeConverter = new(
        type => type.ToWireValue(),
        value => Parse<DependencyType>(value));

    public static readonly ValueConverter<ConsumptionMethod, string> ConsumptionMethodConverter = new(
        method => method.ToWireValue(),
        value => Parse<ConsumptionMethod>(value));

    /// <summary>'in_progress' → <c>InProgress</c>; 'sensor_reading' → <c>SensorReading</c>: the storage underscores are dropped.</summary>
    private static TEnum Parse<TEnum>(string value)
        where TEnum : struct, Enum
        => Enum.Parse<TEnum>(value.Replace("_", string.Empty), ignoreCase: true);
}
