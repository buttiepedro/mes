using Nexo.BuildingBlocks.Domain;

namespace Nexo.Production.Domain;

/// <summary>
/// Value object representing a non-negative produced/scrapped quantity.
/// Enforces the invariant <c>Value &gt;= 0</c> at construction time.
/// </summary>
public sealed class Quantity : ValueObject
{
    public static readonly Quantity Zero = new(0m);

    public decimal Value { get; }

    private Quantity(decimal value) => Value = value;

    /// <summary>Creates a <see cref="Quantity"/>, rejecting negative values.</summary>
    /// <exception cref="ArgumentOutOfRangeException">When <paramref name="value"/> is negative.</exception>
    public static Quantity Of(decimal value)
    {
        if (value < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Quantity cannot be negative.");
        }

        return new Quantity(value);
    }

    public static Quantity operator +(Quantity left, Quantity right) => new(left.Value + right.Value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
