using FluentAssertions;
using Nexo.MasterData.Domain;
using Xunit;

namespace Nexo.MasterData.Tests;

public class UomTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.0001)]
    public void Create_WithNonPositiveFactor_ShouldThrow(double rawFactor)
    {
        var act = () => Uom.Create("kg", "Kilogram", "kg", UomMagnitude.Mass, (decimal)rawFactor);

        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*greater than zero*");
    }

    [Fact]
    public void Create_WithEmptyCode_ShouldThrow()
    {
        var act = () => Uom.Create("", "Kilogram", "kg", UomMagnitude.Mass, 1m);

        act.Should().Throw<ArgumentException>().WithMessage("*Code is required*");
    }

    [Fact]
    public void Create_WithEmptySymbol_ShouldThrow()
    {
        var act = () => Uom.Create("kg", "Kilogram", "  ", UomMagnitude.Mass, 1m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldRaiseUpsertedEventAndDefaultToActiveLocal()
    {
        var uom = Uom.Create("kg", "Kilogram", "kg", UomMagnitude.Mass, 1m, isBase: true);

        uom.Status.Should().Be(MasterStatus.Active);
        uom.Governance.Should().Be(MasterGovernance.Local);
        uom.IsBase.Should().BeTrue();
        uom.FactorToBase.Should().Be(1m);

        var domainEvent = uom.DomainEvents.OfType<MasterRecordUpsertedDomainEvent>().Should().ContainSingle().Subject;
        domainEvent.Catalog.Should().Be(MasterCatalog.Uoms);
        domainEvent.Code.Should().Be("kg");
        domainEvent.Name.Should().Be("Kilogram");
        domainEvent.Change.Should().Be(MasterRecordChange.Created);
    }

    [Fact]
    public void ToBase_ShouldApplyTheFactorWithinTheSameMagnitude()
    {
        var gram = Uom.Create("g", "Gram", "g", UomMagnitude.Mass, 0.001m);

        gram.ToBase(2500m).Should().Be(2.5m);
    }

    [Fact]
    public void Create_WithOutOfRangeDecimals_ShouldThrow()
    {
        var act = () => Uom.Create("kg", "Kilogram", "kg", UomMagnitude.Mass, 1m, decimals: 12);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Archive_ShouldSetArchivedStatusAndRaiseArchivedEvent()
    {
        var uom = Uom.Create("kg", "Kilogram", "kg", UomMagnitude.Mass, 1m);
        uom.ClearDomainEvents();

        uom.Archive();

        uom.Status.Should().Be(MasterStatus.Archived);
        uom.DomainEvents.OfType<MasterRecordArchivedDomainEvent>().Should().ContainSingle()
            .Which.Catalog.Should().Be(MasterCatalog.Uoms);
    }
}
