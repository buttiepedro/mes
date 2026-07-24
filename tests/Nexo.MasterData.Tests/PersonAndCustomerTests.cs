using FluentAssertions;
using Nexo.MasterData.Domain;
using Xunit;

namespace Nexo.MasterData.Tests;

public class PersonAndCustomerTests
{
    [Fact]
    public void Person_Create_WithEmptyCode_ShouldThrow()
    {
        var act = () => Person.Create("", "Ana Pérez");

        act.Should().Throw<ArgumentException>().WithMessage("*Code is required*");
    }

    [Fact]
    public void Person_Create_WithEmptyFullName_ShouldThrow()
    {
        var act = () => Person.Create("L-001", "   ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Person_MayExistWithoutUserAccount()
    {
        var person = Person.Create("L-001", "Ana Pérez");

        person.UserId.Should().BeNull();
        person.Status.Should().Be(MasterStatus.Active);
        person.DomainEvents.OfType<MasterRecordUpsertedDomainEvent>().Should().ContainSingle()
            .Which.Catalog.Should().Be(MasterCatalog.People);
    }

    [Fact]
    public void Person_Archive_ShouldRaiseArchivedEvent()
    {
        var person = Person.Create("L-001", "Ana Pérez");
        person.ClearDomainEvents();

        person.Archive();

        person.Status.Should().Be(MasterStatus.Archived);
        person.DomainEvents.OfType<MasterRecordArchivedDomainEvent>().Should().ContainSingle()
            .Which.Code.Should().Be("L-001");
    }

    [Fact]
    public void Customer_Create_WithEmptyCode_ShouldThrow()
    {
        var act = () => Customer.Create("", "Acme S.A.");

        act.Should().Throw<ArgumentException>().WithMessage("*Code is required*");
    }

    [Fact]
    public void Customer_Create_ShouldRaiseUpsertedEvent()
    {
        var customer = Customer.Create("C-001", "Acme S.A.", taxId: "30-12345678-9");

        customer.LegalName.Should().Be("Acme S.A.");
        customer.TaxId.Should().Be("30-12345678-9");

        var domainEvent = customer.DomainEvents.OfType<MasterRecordUpsertedDomainEvent>().Should().ContainSingle().Subject;
        domainEvent.Catalog.Should().Be(MasterCatalog.Customers);
        domainEvent.Name.Should().Be("Acme S.A.");
    }

    [Fact]
    public void Customer_Update_OnArchivedRecord_ShouldThrow()
    {
        var customer = Customer.Create("C-001", "Acme S.A.");
        customer.Archive();

        var act = () => customer.Update("Acme S.R.L.");

        act.Should().Throw<InvalidOperationException>();
    }
}
