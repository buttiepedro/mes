using FluentAssertions;
using Nexo.MasterData.Domain;
using Xunit;

namespace Nexo.MasterData.Tests;

public class ItemTests
{
    private static readonly Guid BaseUomId = Guid.NewGuid();

    [Fact]
    public void Create_WithoutRoles_ShouldThrow()
    {
        var act = () => Item.Create("SKU-1", "Widget", BaseUomId, Array.Empty<ItemRole>());

        act.Should().Throw<ArgumentException>().WithMessage("*at least one role*");
    }

    [Fact]
    public void Create_WithEmptyCode_ShouldThrow()
    {
        var act = () => Item.Create("   ", "Widget", BaseUomId, new[] { ItemRole.Product });

        act.Should().Throw<ArgumentException>().WithMessage("*Code is required*");
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        var act = () => Item.Create("SKU-1", "", BaseUomId, new[] { ItemRole.Product });

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldTrimCodeAndRaiseUpsertedEvent()
    {
        var item = Item.Create(" SKU-1 ", "Widget", BaseUomId, new[] { ItemRole.Product, ItemRole.Input });

        item.Code.Should().Be("SKU-1");
        item.Status.Should().Be(MasterStatus.Active);
        item.Governance.Should().Be(MasterGovernance.Local);
        item.Roles.Should().BeEquivalentTo(new[] { ItemRole.Product, ItemRole.Input });

        var domainEvent = item.DomainEvents.OfType<MasterRecordUpsertedDomainEvent>().Should().ContainSingle().Subject;
        domainEvent.Catalog.Should().Be(MasterCatalog.Items);
        domainEvent.RecordId.Should().Be(item.Id);
        domainEvent.Code.Should().Be("SKU-1");
        domainEvent.Change.Should().Be(MasterRecordChange.Created);
    }

    [Fact]
    public void Create_WithDuplicatedRoles_ShouldDeduplicate()
    {
        var item = Item.Create("SKU-1", "Widget", BaseUomId, new[] { ItemRole.Input, ItemRole.Input });

        item.Roles.Should().ContainSingle().Which.Should().Be(ItemRole.Input);
        item.HasRole(ItemRole.Product).Should().BeFalse();
    }

    [Fact]
    public void Archive_ShouldSetArchivedStatusAndRaiseArchivedEvent()
    {
        var item = Item.Create("SKU-1", "Widget", BaseUomId, new[] { ItemRole.Product });
        item.ClearDomainEvents();

        item.Archive();

        item.Status.Should().Be(MasterStatus.Archived);
        item.IsArchived.Should().BeTrue();

        var domainEvent = item.DomainEvents.OfType<MasterRecordArchivedDomainEvent>().Should().ContainSingle().Subject;
        domainEvent.Catalog.Should().Be(MasterCatalog.Items);
        domainEvent.RecordId.Should().Be(item.Id);
        domainEvent.Code.Should().Be("SKU-1");
    }

    [Fact]
    public void Archive_Twice_ShouldThrow()
    {
        var item = Item.Create("SKU-1", "Widget", BaseUomId, new[] { ItemRole.Product });
        item.Archive();

        var act = () => item.Archive();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Update_ShouldReplaceRolesAndRaiseUpdatedEvent()
    {
        var item = Item.Create("SKU-1", "Widget", BaseUomId, new[] { ItemRole.Input });
        item.ClearDomainEvents();

        item.Update("Widget v2", new[] { ItemRole.Product }, TrackingMode.Batch, family: "finished");

        item.Name.Should().Be("Widget v2");
        item.Roles.Should().ContainSingle().Which.Should().Be(ItemRole.Product);
        item.Tracking.Should().Be(TrackingMode.Batch);
        item.Family.Should().Be("finished");

        item.DomainEvents.OfType<MasterRecordUpsertedDomainEvent>().Should().ContainSingle()
            .Which.Change.Should().Be(MasterRecordChange.Updated);
    }

    [Fact]
    public void Update_WithoutRoles_ShouldThrow()
    {
        var item = Item.Create("SKU-1", "Widget", BaseUomId, new[] { ItemRole.Input });

        var act = () => item.Update("Widget v2", Array.Empty<ItemRole>(), TrackingMode.None);

        act.Should().Throw<ArgumentException>().WithMessage("*at least one role*");
    }

    [Fact]
    public void Update_OnArchivedItem_ShouldThrow()
    {
        var item = Item.Create("SKU-1", "Widget", BaseUomId, new[] { ItemRole.Input });
        item.Archive();

        var act = () => item.Update("Widget v2", new[] { ItemRole.Input }, TrackingMode.None);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Create_WithNonPositiveIdealCycleTime_ShouldThrow()
    {
        var act = () => Item.Create(
            "SKU-1",
            "Widget",
            BaseUomId,
            new[] { ItemRole.Product },
            idealCycleTime: 0m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
