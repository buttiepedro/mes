using Nexo.BuildingBlocks.Domain;

namespace Nexo.MasterData.Domain;

/// <summary>
/// Unit of measure (<c>master.uom</c>). Aggregate root.
/// </summary>
/// <remarks>
/// Hard rule (master-data.md §2.4): <b>magnitudes are never converted between each other</b>.
/// <see cref="FactorToBase"/> only applies <b>within</b> the same <see cref="Magnitude"/>; going from
/// kg to units requires the item's unit weight, not a unit conversion. A factor that already valued
/// history is not edited — it is versioned with a validity period (DS-11), which is why there is no
/// setter for it.
/// </remarks>
public sealed class Uom : MasterRecord
{
    // EF Core materialization constructor.
    private Uom()
    {
        Name = string.Empty;
        Symbol = string.Empty;
    }

    private Uom(
        Guid id,
        string code,
        string name,
        string symbol,
        UomMagnitude? magnitude,
        decimal factorToBase,
        bool isBase,
        short decimals,
        MasterGovernance governance,
        string? externalRef)
        : base(id, code, governance, externalRef)
    {
        Name = NormalizeRequired(name, nameof(name));
        Symbol = NormalizeRequired(symbol, nameof(symbol));
        Magnitude = magnitude;
        FactorToBase = EnsureFactor(factorToBase);
        IsBase = isBase;
        Decimals = EnsureDecimals(decimals);
    }

    public override string Catalog => MasterCatalog.Uoms;

    public override string DisplayName => Name;

    public string Name { get; private set; }

    /// <summary>Short symbol shown next to quantities (<c>kg</c>, <c>u</c>, <c>l</c>).</summary>
    public string Symbol { get; private set; }

    /// <summary>Physical magnitude; <c>null</c> for mirrored rows whose magnitude the ERP does not declare.</summary>
    public UomMagnitude? Magnitude { get; private set; }

    /// <summary>Conversion factor to the base unit of the same magnitude. Always greater than zero.</summary>
    public decimal FactorToBase { get; private set; }

    /// <summary>Whether this is the base unit of its magnitude (at most one per magnitude).</summary>
    public bool IsBase { get; private set; }

    /// <summary>Aggregation precision, so totals are reproducible.</summary>
    public short Decimals { get; private set; }

    /// <summary>
    /// Creates a unit of measure and raises the upserted domain event.
    /// </summary>
    /// <exception cref="ArgumentException">When the code, name or symbol are empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">When <paramref name="factorToBase"/> is not greater than zero.</exception>
    public static Uom Create(
        string code,
        string name,
        string symbol,
        UomMagnitude? magnitude,
        decimal factorToBase,
        bool isBase = false,
        short decimals = 4,
        MasterGovernance governance = MasterGovernance.Local,
        string? externalRef = null)
    {
        var uom = new Uom(
            UuidV7.NewGuid(),
            code,
            name,
            symbol,
            magnitude,
            factorToBase,
            isBase,
            decimals,
            governance,
            externalRef);

        uom.RaiseUpserted(MasterRecordChange.Created);

        return uom;
    }

    /// <summary>Updates the descriptive (non-valuing) attributes and raises the upserted domain event.</summary>
    public void Update(string name, string symbol, short decimals)
    {
        Name = NormalizeRequired(name, nameof(name));
        Symbol = NormalizeRequired(symbol, nameof(symbol));
        Decimals = EnsureDecimals(decimals);
        Touch();

        RaiseUpserted(MasterRecordChange.Updated);
    }

    /// <summary>
    /// Converts a value expressed in this unit to the base unit of its magnitude.
    /// </summary>
    public decimal ToBase(decimal value) => value * FactorToBase;

    private static decimal EnsureFactor(decimal factorToBase)
    {
        if (factorToBase <= 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(factorToBase),
                factorToBase,
                "The conversion factor to the base unit must be greater than zero.");
        }

        return factorToBase;
    }

    private static short EnsureDecimals(short decimals)
    {
        if (decimals is < 0 or > 9)
        {
            throw new ArgumentOutOfRangeException(
                nameof(decimals),
                decimals,
                "Decimal precision must be between 0 and 9.");
        }

        return decimals;
    }
}
