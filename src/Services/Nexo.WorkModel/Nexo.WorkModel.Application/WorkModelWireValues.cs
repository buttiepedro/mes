using Nexo.WorkModel.Domain;

namespace Nexo.WorkModel.Application;

/// <summary>
/// Translation between the domain enums and the lower-case snake_case values used by the REST
/// contract and by the <c>work</c> schema (<c>repetitive</c>, <c>published</c>, <c>per_unit</c>, ...).
/// </summary>
/// <remarks>
/// The REST contract in docs/design/04-service-contracts.md §2.6 spells the enum members in Spanish
/// (<c>repetitivo</c>, <c>Publicada</c>); the authoritative <b>storage</b> values in
/// docs/design/03-data-schema.md §2.6 are English (<c>repetitive</c>, <c>published</c>). The API
/// speaks the storage values so that wire and column never diverge — the same call MasterData made.
/// <para>
/// It lives in the Application layer (and is reused by the Infrastructure value converters, which
/// reference it) so a single table defines the vocabulary.
/// </para>
/// </remarks>
public static class WorkModelWireValues
{
    public static string ToWireValue(this ProcessProfile profile) => profile switch
    {
        ProcessProfile.Repetitive => "repetitive",
        ProcessProfile.Project => "project",
        _ => profile.ToString().ToLowerInvariant()
    };

    public static string ToWireValue(this ProcessStatus status) => status.ToString().ToLowerInvariant();

    public static string ToWireValue(this ProcessVersionState state) => state.ToString().ToLowerInvariant();

    /// <summary>FS | SS | FF — upper case, exactly as <c>ck_task_dep_type</c> spells them.</summary>
    public static string ToWireValue(this DependencyType type) => type.ToString();

    public static string ToWireValue(this CompletionKind kind) => ToSnakeCase(kind.ToString());

    public static string ToWireValue(this TaskObligation obligation) => obligation.ToString().ToLowerInvariant();

    public static string ToWireValue(this EvidencePolicy policy) => policy.ToString().ToLowerInvariant();

    public static string ToWireValue(this EvidenceKind kind) => ToSnakeCase(kind.ToString());

    public static string ToWireValue(this SkipPolicy policy) => policy.ToString().ToLowerInvariant();

    public static string ToWireValue(this InputBasis basis) => ToSnakeCase(basis.ToString());

    public static string ToWireValue(this InputKind kind) => ToSnakeCase(kind.ToString());

    public static string ToWireValue(this ValidationSeverity severity) => severity.ToString().ToLowerInvariant();

    public static bool TryParseProfile(string? value, out ProcessProfile profile)
        => TryParse(value, out profile);

    public static bool TryParseProcessStatus(string? value, out ProcessStatus status)
        => TryParse(value, out status);

    public static bool TryParseVersionState(string? value, out ProcessVersionState state)
        => TryParse(value, out state);

    public static bool TryParseDependencyType(string? value, out DependencyType type)
        => TryParse(value, out type);

    public static bool TryParseCompletionKind(string? value, out CompletionKind kind)
        => TryParse(value, out kind);

    public static bool TryParseObligation(string? value, out TaskObligation obligation)
        => TryParse(value, out obligation);

    public static bool TryParseEvidencePolicy(string? value, out EvidencePolicy policy)
        => TryParse(value, out policy);

    public static bool TryParseEvidenceKind(string? value, out EvidenceKind kind)
        => TryParse(value, out kind);

    public static bool TryParseSkipPolicy(string? value, out SkipPolicy policy)
        => TryParse(value, out policy);

    public static bool TryParseInputBasis(string? value, out InputBasis basis)
        => TryParse(value, out basis);

    public static bool TryParseInputKind(string? value, out InputKind kind)
        => TryParse(value, out kind);

    public static bool TryParseVersionBump(string? value, out VersionBump bump)
        => TryParse(value, out bump);

    /// <summary>
    /// Parses a wire value into <typeparamref name="TEnum"/>. Snake_case is accepted transparently
    /// (<c>per_unit</c> → <c>PerUnit</c>), so the API and the columns can speak the same words.
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

    /// <summary>'ExternalLabor' → 'external_labor'.</summary>
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
