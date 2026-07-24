using Nexo.MasterData.Domain;

namespace Nexo.MasterData.Application;

/// <summary>
/// Translation between the domain enums and the lower-case wire/storage values used by the REST
/// contract and by the <c>master</c> schema (<c>product</c>, <c>input</c>, <c>batch</c>, <c>mass</c>, ...).
/// </summary>
/// <remarks>
/// The REST contract in docs/design/04-service-contracts.md §2.5 spells the enum members in Spanish
/// (<c>producto</c>, <c>insumo</c>, <c>lote</c>); the authoritative <b>storage</b> values in
/// docs/design/03-data-schema.md §2.5.2 are English (<c>product</c>, <c>input</c>, <c>batch</c>).
/// The API speaks the storage values so that wire and column never diverge.
/// </remarks>
public static class MasterDataWireValues
{
    public static string ToWireValue(this ItemRole role) => role switch
    {
        ItemRole.Product => "product",
        ItemRole.Input => "input",
        _ => role.ToString().ToLowerInvariant()
    };

    public static string ToWireValue(this TrackingMode tracking) => tracking.ToString().ToLowerInvariant();

    public static string ToWireValue(this UomMagnitude magnitude) => magnitude.ToString().ToLowerInvariant();

    public static string ToWireValue(this MasterStatus status) => status.ToString().ToLowerInvariant();

    public static string ToWireValue(this MasterGovernance governance) => governance.ToString().ToLowerInvariant();

    public static bool TryParseRole(string? value, out ItemRole role)
        => Enum.TryParse(value, ignoreCase: true, out role) && Enum.IsDefined(role);

    public static bool TryParseTracking(string? value, out TrackingMode tracking)
        => Enum.TryParse(value, ignoreCase: true, out tracking) && Enum.IsDefined(tracking);

    public static bool TryParseMagnitude(string? value, out UomMagnitude magnitude)
        => Enum.TryParse(value, ignoreCase: true, out magnitude) && Enum.IsDefined(magnitude);

    public static bool TryParseStatus(string? value, out MasterStatus status)
        => Enum.TryParse(value, ignoreCase: true, out status) && Enum.IsDefined(status);

    /// <summary>Parses a role list, returning <c>null</c> when any entry is unknown.</summary>
    public static IReadOnlyList<ItemRole>? ParseRoles(IEnumerable<string>? values)
    {
        if (values is null)
        {
            return null;
        }

        var roles = new List<ItemRole>();

        foreach (var value in values)
        {
            if (!TryParseRole(value, out var role))
            {
                return null;
            }

            roles.Add(role);
        }

        return roles;
    }
}
