using FluentAssertions;
using Nexo.Production.Domain;
using Xunit;

namespace Nexo.Production.Tests;

public class ProductionRunTests
{
    [Fact]
    public void Register_ShouldAppendRecordAndRaiseProductionRegisteredEvent()
    {
        var run = ProductionRun.Open(workOrderId: Guid.NewGuid(), machineId: Guid.NewGuid(), shiftId: Guid.NewGuid());

        var record = run.Register(Quantity.Of(10m), Quantity.Of(2m), operatorId: Guid.NewGuid(), ProductionSource.Manual);

        run.Records.Should().ContainSingle().Which.Should().BeSameAs(record);

        var domainEvent = run.DomainEvents.OfType<ProductionRegisteredDomainEvent>().Should().ContainSingle().Subject;
        domainEvent.RunId.Should().Be(run.Id);
        domainEvent.WorkOrderId.Should().Be(run.WorkOrderId);
        domainEvent.GoodQty.Should().Be(10m);
        domainEvent.ScrapQty.Should().Be(2m);
    }

    [Fact]
    public void Close_ShouldSetClosedStatusAndRaiseRunClosedEvent()
    {
        var run = ProductionRun.Open(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        run.Close();

        run.Status.Should().Be(RunStatus.Closed);
        run.ClosedAt.Should().NotBeNull();
        run.DomainEvents.OfType<RunClosedDomainEvent>().Should().ContainSingle()
            .Which.RunId.Should().Be(run.Id);
    }

    [Fact]
    public void Register_OnClosedRun_ShouldThrow()
    {
        var run = ProductionRun.Open(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        run.Close();

        var act = () => run.Register(Quantity.Of(1m), Quantity.Zero, Guid.NewGuid(), ProductionSource.Manual);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Close_Twice_ShouldThrow()
    {
        var run = ProductionRun.Open(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        run.Close();

        var act = () => run.Close();

        act.Should().Throw<InvalidOperationException>();
    }
}
