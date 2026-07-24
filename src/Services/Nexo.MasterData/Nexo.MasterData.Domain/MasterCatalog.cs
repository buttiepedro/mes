namespace Nexo.MasterData.Domain;

/// <summary>
/// Canonical catalog names carried by the master-data integration events and used by the
/// CSV import templates (docs/design/04-service-contracts.md §2.5).
/// </summary>
public static class MasterCatalog
{
    public const string Uoms = "uoms";

    public const string Items = "items";

    public const string People = "people";

    public const string Customers = "customers";
}
