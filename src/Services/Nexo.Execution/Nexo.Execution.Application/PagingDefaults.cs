namespace Nexo.Execution.Application;

/// <summary>
/// Paging limits shared by the list queries (docs/design/04-service-contracts.md §1.2:
/// <c>limit</c> defaults to 50 and never exceeds 200).
/// </summary>
public static class PagingDefaults
{
    public const int DefaultLimit = 50;

    public const int MaxLimit = 200;

    /// <summary>Clamps a requested page size into the allowed range.</summary>
    public static int Clamp(int limit) => limit switch
    {
        <= 0 => DefaultLimit,
        > MaxLimit => MaxLimit,
        _ => limit
    };

    /// <summary>Normalizes a requested offset, never returning a negative value.</summary>
    public static int NormalizeOffset(int offset) => offset < 0 ? 0 : offset;
}
