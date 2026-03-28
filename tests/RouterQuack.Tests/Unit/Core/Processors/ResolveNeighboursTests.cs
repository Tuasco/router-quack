using Microsoft.Extensions.Logging;
using NSubstitute;
using RouterQuack.Core.Processors;
using RouterQuack.Tests.Unit.TestHelpers;
using Context = RouterQuack.Core.Models.Context;

namespace RouterQuack.Tests.Unit.Core.Processors;

public class ResolveNeighboursTests
{
    private readonly ILogger<ResolveNeighbours> _logger = Substitute.For<ILogger<ResolveNeighbours>>();

    [Test]
    public async Task Process_ShortSameAsNeighbourWithoutConflict_ResolvesNeighbour()
    {
        var r1Interface = CreatePendingInterface("GigabitEthernet1/0", "111:R2:");
        var r2Interface = CreatePendingInterface("GigabitEthernet1/0", "111:R1:");

        var context = CreateContext(
            TestData.CreateAs(number: 111, routers:
            [
                TestData.CreateRouter(name: "R1", interfaces: [r1Interface]),
                TestData.CreateRouter(name: "R2", interfaces: [r2Interface])
            ]));

        var processor = new ResolveNeighbours(_logger, context);

        processor.Process();

        await Assert.That(processor.Context.ErrorsOccurred).IsFalse();
        await Assert.That(r1Interface.Neighbour).IsEqualTo(r2Interface);
        await Assert.That(r2Interface.Neighbour).IsEqualTo(r1Interface);
    }

    [Test]
    public async Task Process_ShortNeighbourWithParallelLinks_SetsErrorsOccurred()
    {
        var r1Interface1 = CreatePendingInterface("GigabitEthernet1/0", "111:R2:");
        var r1Interface2 = CreatePendingInterface("GigabitEthernet2/0", "111:R2:");
        var r3Interface1 = CreatePendingInterface("GigabitEthernet1/0", "111:R1:");
        var r3Interface2 = CreatePendingInterface("GigabitEthernet2/0", "111:R1:");

        var context = CreateContext(
            TestData.CreateAs(number: 111, routers:
            [
                TestData.CreateRouter(name: "R1", interfaces: [r1Interface1, r1Interface2]),
                TestData.CreateRouter(name: "R2", interfaces: [r3Interface1, r3Interface2])
            ]));

        var processor = new ResolveNeighbours(_logger, context);

        processor.Process();

        await Assert.That(processor.Context.ErrorsOccurred).IsTrue();
        await Assert.That(r1Interface1.Neighbour).IsNull();
        await Assert.That(r1Interface2.Neighbour).IsNull();
        await Assert.That(r3Interface1.Neighbour).IsNull();
        await Assert.That(r3Interface2.Neighbour).IsNull();
    }

    [Test]
    public async Task Process_ExplicitSameAsNeighbourWithParallelLinks_ResolvesEachInterfaceToUniquePeer()
    {
        var r1Interface1 = CreatePendingInterface("GigabitEthernet1/0", "111:R2:GigabitEthernet1/0");
        var r1Interface2 = CreatePendingInterface("GigabitEthernet2/0", "111:R2:GigabitEthernet2/0");
        var r2Interface1 = CreatePendingInterface("GigabitEthernet1/0", "111:R1:GigabitEthernet1/0");
        var r2Interface2 = CreatePendingInterface("GigabitEthernet2/0", "111:R1:GigabitEthernet2/0");

        var context = CreateContext(
            TestData.CreateAs(number: 111, routers:
            [
                TestData.CreateRouter(name: "R1", interfaces: [r1Interface1, r1Interface2]),
                TestData.CreateRouter(name: "R2", interfaces: [r2Interface1, r2Interface2])
            ]));

        var processor = new ResolveNeighbours(_logger, context);

        processor.Process();

        await Assert.That(processor.Context.ErrorsOccurred).IsFalse();
        await Assert.That(r1Interface1.Neighbour).IsEqualTo(r2Interface1);
        await Assert.That(r1Interface2.Neighbour).IsEqualTo(r2Interface2);
        await Assert.That(r2Interface1.Neighbour).IsEqualTo(r1Interface1);
        await Assert.That(r2Interface2.Neighbour).IsEqualTo(r1Interface2);
    }

    [Test]
    public async Task Process_MinimalSameAsNeighbourWithParallelLinks_ResolvesEachInterfaceToUniquePeer()
    {
        var r1Interface1 = CreatePendingInterface("GigabitEthernet1/0", "111:R2:GigabitEthernet1/0");
        var r1Interface2 = CreatePendingInterface("GigabitEthernet2/0", "111:R2:");
        var r2Interface1 = CreatePendingInterface("GigabitEthernet1/0", "111:R1:");
        var r2Interface2 = CreatePendingInterface("GigabitEthernet2/0", "111:R1:");

        var context = CreateContext(
            TestData.CreateAs(number: 111, routers:
            [
                TestData.CreateRouter(name: "R1", interfaces: [r1Interface1, r1Interface2]),
                TestData.CreateRouter(name: "R2", interfaces: [r2Interface1, r2Interface2])
            ]));

        var processor = new ResolveNeighbours(_logger, context);

        processor.Process();

        await Assert.That(processor.Context.ErrorsOccurred).IsFalse();
        await Assert.That(r1Interface1.Neighbour).IsEqualTo(r2Interface1);
        await Assert.That(r1Interface2.Neighbour).IsEqualTo(r2Interface2);
        await Assert.That(r2Interface1.Neighbour).IsEqualTo(r1Interface1);
        await Assert.That(r2Interface2.Neighbour).IsEqualTo(r1Interface2);
    }

    [Test]
    public async Task Process_ExplicitCrossAsNeighbour_ResolvesNeighbour()
    {
        var r1Interface = CreatePendingInterface("GigabitEthernet1/0", "112:R2:GigabitEthernet1/0");
        var r2Interface = CreatePendingInterface("GigabitEthernet1/0", "111:R1:GigabitEthernet1/0");

        var context = CreateContext(
            TestData.CreateAs(number: 111, routers: [TestData.CreateRouter(name: "R1", interfaces: [r1Interface])]),
            TestData.CreateAs(number: 112, routers: [TestData.CreateRouter(name: "R2", interfaces: [r2Interface])]));

        var processor = new ResolveNeighbours(_logger, context);

        processor.Process();

        await Assert.That(processor.Context.ErrorsOccurred).IsFalse();
        await Assert.That(r1Interface.Neighbour).IsEqualTo(r2Interface);
        await Assert.That(r2Interface.Neighbour).IsEqualTo(r1Interface);
    }

    private static Context CreateContext(params As[] asses)
        => ContextFactory.Create(asses: asses);

    private static Interface CreatePendingInterface(string name, string neighbourName)
    {
        var dummyNeighbour = TestData.CreateInterface(name: neighbourName, neighbour: null);

        return TestData.CreateInterface(name: name, neighbour: dummyNeighbour);
    }
}