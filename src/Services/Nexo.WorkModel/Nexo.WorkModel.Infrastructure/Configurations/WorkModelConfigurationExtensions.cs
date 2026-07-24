using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Nexo.WorkModel.Application;
using Nexo.WorkModel.Domain;

namespace Nexo.WorkModel.Infrastructure.Configurations;

/// <summary>
/// Shared mapping pieces of the <c>work</c> schema: the "live rows" filter used by every partial
/// unique index and the value converters that persist the domain enums as the lower-case text the
/// design's CHECK constraints spell (docs/design/03-data-schema.md §1.4 and §2.6).
/// </summary>
/// <remarks>
/// The converters delegate to <see cref="WorkModelWireValues"/> so the storage value and the wire
/// value are the same string by construction and can never drift apart.
/// </remarks>
internal static class WorkModelConfigurationExtensions
{
    /// <summary>Filter of every partial unique index over a natural key: only live rows compete.</summary>
    public const string LiveRowsFilter = "deleted_at IS NULL";

    public static readonly ValueConverter<ProcessProfile, string> ProfileConverter = new(
        profile => profile.ToWireValue(),
        value => Parse<ProcessProfile>(value));

    public static readonly ValueConverter<ProcessStatus, string> ProcessStatusConverter = new(
        status => status.ToWireValue(),
        value => Parse<ProcessStatus>(value));

    public static readonly ValueConverter<ProcessVersionState, string> VersionStateConverter = new(
        state => state.ToWireValue(),
        value => Parse<ProcessVersionState>(value));

    public static readonly ValueConverter<DependencyType, string> DependencyTypeConverter = new(
        type => type.ToWireValue(),
        value => Parse<DependencyType>(value));

    public static readonly ValueConverter<CompletionKind, string> CompletionKindConverter = new(
        kind => kind.ToWireValue(),
        value => Parse<CompletionKind>(value));

    public static readonly ValueConverter<TaskObligation, string> ObligationConverter = new(
        obligation => obligation.ToWireValue(),
        value => Parse<TaskObligation>(value));

    public static readonly ValueConverter<EvidencePolicy, string> EvidencePolicyConverter = new(
        policy => policy.ToWireValue(),
        value => Parse<EvidencePolicy>(value));

    public static readonly ValueConverter<EvidenceKind, string> EvidenceKindConverter = new(
        kind => kind.ToWireValue(),
        value => Parse<EvidenceKind>(value));

    public static readonly ValueConverter<SkipPolicy, string> SkipPolicyConverter = new(
        policy => policy.ToWireValue(),
        value => Parse<SkipPolicy>(value));

    public static readonly ValueConverter<InputBasis, string> InputBasisConverter = new(
        basis => basis.ToWireValue(),
        value => Parse<InputBasis>(value));

    public static readonly ValueConverter<InputKind, string> InputKindConverter = new(
        kind => kind.ToWireValue(),
        value => Parse<InputKind>(value));

    /// <summary>'per_unit' → <c>PerUnit</c>; the underscores of the storage vocabulary are dropped.</summary>
    private static TEnum Parse<TEnum>(string value)
        where TEnum : struct, Enum
        => Enum.Parse<TEnum>(value.Replace("_", string.Empty), ignoreCase: true);
}
