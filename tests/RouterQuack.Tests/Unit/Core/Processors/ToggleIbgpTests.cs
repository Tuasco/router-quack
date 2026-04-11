using Microsoft.Extensions.Logging;
using NSubstitute;
using RouterQuack.Core.Processors;
using RouterQuack.Tests.Unit.TestHelpers;

namespace RouterQuack.Tests.Unit.Core.Processors;

public class ToggleIbgpTests
{
    private readonly ILogger<ToggleIbgp> _logger = Substitute.For<ILogger<ToggleIbgp>>();

    [Test]
    public async Task Process_RouterInIbgpAs_EnablesIbgp()
    {
        var interfaces = new List<Interface>
        {
            TestData.CreateInterface()
        };
        var routers = new List<Router>
        {
            TestData.CreateRouter(interfaces: interfaces)
        };
        var asses = new List<As>
        {
            TestData.CreateAs(
                core: CoreType.iBGP,
                routers: routers)
        };

        var context = ContextFactory.Create(asses: asses);
        var processor = new ToggleIbgp(_logger, context);
        processor.Process();

        await Assert.That(routers[0].Bgp.Ibgp).IsTrue();
    }

    [Test]
    public async Task Process_RouterInOspfAs_KeepsIbgpDisabled()
    {
        var interfaces = new List<Interface>
        {
            TestData.CreateInterface()
        };
        var routers = new List<Router>
        {
            TestData.CreateRouter(interfaces: interfaces)
        };
        var asses = new List<As>
        {
            TestData.CreateAs(
                igp: IgpType.OSPF,
                routers: routers)
        };

        var context = ContextFactory.Create(asses: asses);
        var processor = new ToggleIbgp(_logger, context);
        processor.Process();

        await Assert.That(routers[0].Bgp.Ibgp).IsFalse();
    }

    [Test]
    public async Task Process_BorderRouterInOspfAs_EnablesIbgp()
    {
        var interfaces = new List<Interface>
        {
            TestData.CreateInterface(bgp: BgpRelationship.Client)
        };
        var routers = new List<Router>
        {
            TestData.CreateRouter(interfaces: interfaces)
        };
        var asses = new List<As>
        {
            TestData.CreateAs(
                igp: IgpType.OSPF,
                routers: routers)
        };

        var context = ContextFactory.Create(asses: asses);
        var processor = new ToggleIbgp(_logger, context);
        processor.Process();

        await Assert.That(routers[0].Bgp.Ibgp).IsTrue();
    }

    [Test]
    public async Task Process_NonBorderRouterInOspfAs_KeepsIbgpDisabled()
    {
        var interfaces = new List<Interface>
        {
            TestData.CreateInterface(bgp: BgpRelationship.None)
        };
        var routers = new List<Router>
        {
            TestData.CreateRouter(interfaces: interfaces)
        };
        var asses = new List<As>
        {
            TestData.CreateAs(
                igp: IgpType.OSPF,
                routers: routers)
        };

        var context = ContextFactory.Create(asses: asses);
        var processor = new ToggleIbgp(_logger, context);
        processor.Process();

        await Assert.That(routers[0].Bgp.Ibgp).IsFalse();
    }

    [Test]
    public async Task Process_MultipleRoutersInDifferentAses_AppliesCorrectly()
    {
        var router1Interfaces = new List<Interface>
        {
            TestData.CreateInterface()
        };
        var router2Interfaces = new List<Interface>
        {
            TestData.CreateInterface()
        };
        var router3Interfaces = new List<Interface>
        {
            TestData.CreateInterface(bgp: BgpRelationship.Peer)
        };

        var routersAs1 = new List<Router>
        {
            TestData.CreateRouter(name: "Router1", interfaces: router1Interfaces)
        };
        var routersAs2 = new List<Router>
        {
            TestData.CreateRouter(name: "Router2", interfaces: router2Interfaces),
            TestData.CreateRouter(name: "Router3", interfaces: router3Interfaces)
        };

        var asses = new List<As>
        {
            TestData.CreateAs(number: 1, core: CoreType.iBGP, routers: routersAs1),
            TestData.CreateAs(number: 2, core: CoreType.None, routers: routersAs2)
        };

        var context = ContextFactory.Create(asses: asses);
        var processor = new ToggleIbgp(_logger, context);
        processor.Process();

        await Assert.That(routersAs1[0].Bgp.Ibgp).IsTrue();
        await Assert.That(routersAs2[0].Bgp.Ibgp).IsFalse();
        await Assert.That(routersAs2[1].Bgp.Ibgp).IsTrue();
    }
}