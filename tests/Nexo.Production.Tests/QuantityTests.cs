using FluentAssertions;
using Nexo.Production.Domain;
using Xunit;

namespace Nexo.Production.Tests;

public class QuantityTests
{
    [Fact]
    public void Of_NegativeValue_ShouldThrow()
    {
        var act = () => Quantity.Of(-1m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(1234.5678)]
    public void Of_NonNegativeValue_ShouldCarryValue(double raw)
    {
        var value = (decimal)raw;

        Quantity.Of(value).Value.Should().Be(value);
    }

    [Fact]
    public void Equality_ShouldBeByValue()
    {
        Quantity.Of(5m).Should().Be(Quantity.Of(5m));
        Quantity.Of(5m).Should().NotBe(Quantity.Of(6m));
    }

    [Fact]
    public void Addition_ShouldSumValues()
    {
        (Quantity.Of(3m) + Quantity.Of(4m)).Value.Should().Be(7m);
    }
}
