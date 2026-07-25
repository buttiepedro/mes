using Nexo.Execution.Domain;

namespace Nexo.Execution.Application;

/// <summary>
/// Translation between the domain enums and the lower-case snake_case values used by the REST contract
/// and by the <c>execution</c> schema (<c>batch</c>, <c>in_progress</c>, <c>work_order</c>, ...).
/// </summary>
/// <remarks>
/// The REST contract in docs/design/04-service-contracts.md §2.7 spells some enum members in Spanish
/// (<c>lote</c>, <c>proyecto</c>); the authoritative <b>storage</b> values in
/// docs/design/03-data-schema.md §2.7 are English (<c>batch</c>, <c>project</c>, <c>work_order</c>). The
/// API speaks the storage values so wire and column never diverge — the same call Work Model / Master Data
/// made. It lives in the Application layer so a single table defines the vocabulary (the Infrastructure
/// value converters reuse it).
/// </remarks>
public static class ExecutionWireValues
{
    public static string ToWireValue(this ExecutionFlavor flavor) => flavor.ToString().ToLowerInvariant();

    public static string ToWireValue(this ExecutionStatus status) => ToSnakeCase(status.ToString());

    public static string ToWireValue(this TaskRunStatus status) => ToSnakeCase(status.ToString());

    public static string ToWireValue(this TriggerKind kind) => ToSnakeCase(kind.ToString());

    /// <summary>FS | SS | FF — upper case, as the model spells them.</summary>
    public static string ToWireValue(this DependencyType type) => type.ToString();

    public static string ToWireValue(this AssignmentMode mode) => ToSnakeCase(mode.ToString());

    public static string ToWireValue(this ProgressMethod method) => ToSnakeCase(method.ToString());

    public static string ToWireValue(this ConsumptionMethod method) => ToSnakeCase(method.ToString());

    public static string ToWireValue(this TaskObligation obligation) => obligation.ToString().ToLowerInvariant();

    public static string ToWireValue(this EvidenceKind kind) => ToSnakeCase(kind.ToString());

    public static string ToWireValue(this EvidenceStatus status) => status.ToString().ToLowerInvariant();

    public static string ToWireValue(this BlockCause cause) => cause.ToString().ToLowerInvariant();

    public static string ToWireValue(this CloseKind kind) => kind.ToString().ToLowerInvariant();

    public static bool TryParseFlavor(string? value, out ExecutionFlavor flavor)
        => TryParse(value, out flavor);

    /// <summary>Derives the flavour from the process profile (E3): <c>repetitive</c> → batch, <c>project</c> → project.</summary>
    public static bool TryParseFlavorFromProfile(string? profile, out ExecutionFlavor flavor)
    {
        flavor = default;

        if (string.IsNullOrWhiteSpace(profile))
        {
            return false;
        }

        switch (profile.Trim().ToLowerInvariant())
        {
            case "repetitive":
            case "batch":
            case "lote":
                flavor = ExecutionFlavor.Batch;
                return true;
            case "project":
            case "proyecto":
                flavor = ExecutionFlavor.Project;
                return true;
            default:
                return false;
        }
    }

    public static bool TryParseExecutionStatus(string? value, out ExecutionStatus status)
        => TryParse(value, out status);

    public static bool TryParseTaskRunStatus(string? value, out TaskRunStatus status)
        => TryParse(value, out status);

    public static bool TryParseTriggerKind(string? value, out TriggerKind kind)
        => TryParse(value, out kind);

    public static bool TryParseDependencyType(string? value, out DependencyType type)
        => TryParse(value, out type);

    public static bool TryParseAssignmentMode(string? value, out AssignmentMode mode)
        => TryParse(value, out mode);

    public static bool TryParseProgressMethod(string? value, out ProgressMethod method)
        => TryParse(value, out method);

    public static bool TryParseConsumptionMethod(string? value, out ConsumptionMethod method)
        => TryParse(value, out method);

    public static bool TryParseObligation(string? value, out TaskObligation obligation)
        => TryParse(value, out obligation);

    public static bool TryParseEvidenceKind(string? value, out EvidenceKind kind)
        => TryParse(value, out kind);

    public static bool TryParseEvidenceStatus(string? value, out EvidenceStatus status)
        => TryParse(value, out status);

    public static bool TryParseBlockCause(string? value, out BlockCause cause)
        => TryParse(value, out cause);

    public static bool TryParseCloseKind(string? value, out CloseKind kind)
        => TryParse(value, out kind);

    /// <summary>
    /// Parses a wire value into <typeparamref name="TEnum"/>. Snake_case is accepted transparently
    /// (<c>in_progress</c> → <c>InProgress</c>), so the API and the columns can speak the same words.
    /// </summary>
    private static bool TryParse<TEnum>(string? value, out TEnum parsed)
        where TEnum : struct, Enum
    {
        parsed = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var candidate = value.Trim().Replace("_", string.Empty);

        return Enum.TryParse(candidate, ignoreCase: true, out parsed) && Enum.IsDefined(parsed);
    }

    /// <summary>'InProgress' → 'in_progress', 'WorkOrder' → 'work_order'.</summary>
    private static string ToSnakeCase(string pascalCase)
    {
        var builder = new System.Text.StringBuilder(pascalCase.Length + 4);

        for (var index = 0; index < pascalCase.Length; index++)
        {
            var character = pascalCase[index];

            if (char.IsUpper(character) && index > 0)
            {
                builder.Append('_');
            }

            builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString();
    }
}
