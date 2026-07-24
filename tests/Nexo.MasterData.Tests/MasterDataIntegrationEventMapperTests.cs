using FluentAssertions;
using Nexo.BuildingBlocks.Messaging;
using Nexo.MasterData.Application;
using Nexo.MasterData.Domain;
using Xunit;

namespace Nexo.MasterData.Tests;

public class MasterDataIntegrationEventMapperTests
{
    [Fact]
    public void Map_UpsertedDomainEvent_ShouldCarryTheCanonicalType()
    {
        var item = Item.Create("SKU-1", "Widget", Guid.NewGuid(), new[] { ItemRole.Product });
        var domainEvent = item.DomainEvents.OfType<MasterRecordUpsertedDomainEvent>().Single();

        var integrationEvent = MasterDataIntegrationEventMapper.Map(domainEvent);

        integrationEvent.Should().BeOfType<MasterDataRecordUpsertedIntegrationEvent>();
        integrationEvent!.Type.Should().Be(EventTypes.MasterData_RecordUpserted);
        integrationEvent.Type.Should().Be("nexo.masterdata.record_upserted");

        var upserted = (MasterDataRecordUpsertedIntegrationEvent)integrationEvent;
        upserted.Catalog.Should().Be(MasterCatalog.Items);
        upserted.RecordId.Should().Be(item.Id);
        upserted.Change.Should().Be("created");
    }

    [Fact]
    public void Map_ArchivedDomainEvent_ShouldCarryTheCanonicalType()
    {
        var customer = Customer.Create("C-001", "Acme S.A.");
        customer.Archive();
        var domainEvent = customer.DomainEvents.OfType<MasterRecordArchivedDomainEvent>().Single();

        var integrationEvent = MasterDataIntegrationEventMapper.Map(domainEvent);

        integrationEvent.Should().BeOfType<MasterDataRecordArchivedIntegrationEvent>();
        integrationEvent!.Type.Should().Be(EventTypes.MasterData_RecordArchived);
        integrationEvent.Type.Should().Be("nexo.masterdata.record_archived");
    }
}
