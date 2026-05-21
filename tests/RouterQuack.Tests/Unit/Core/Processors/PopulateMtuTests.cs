using Microsoft.Extensions.Logging;
using NSubstitute;
using RouterQuack.Core.Processors;
using RouterQuack.Tests.Unit.TestHelpers;

namespace RouterQuack.Tests.Unit.Core.Processors;

public class PopulateMtuTests
{
    private readonly ILogger<PopulateMtu> _logger = Substitute.For<ILogger<PopulateMtu>>();

    [Test]
    public async Task Process_NoLdpNoMtu_SetsMtuTo1500()
    {
        var localInterface = CreateLinkedInterfaces(sameAs: true);
        var routers = new List<Router>
        {
            TestData.CreateRouter(name: "R1", interfaces: [localInterface])
        };
        var asses = new List<As>
        {
            TestData.CreateAs(number: 1, core: CoreType.None, routers: routers)
        };

        var context = ContextFactory.Create(asses: asses);
        var processor = new PopulateMtu(_logger, context);
        processor.Process();

        await Assert.That(localInterface.Mtu).IsEqualTo(1500);
    }

    [Test]
    public async Task Process_NoLdpWithCustomMtu_NoOverwrite()
    {
        var localInterface = CreateLinkedInterfaces(sameAs: true, mtu: 9000);
        var routers = new List<Router>
        {
            TestData.CreateRouter(name: "R1", interfaces: [localInterface])
        };
        var asses = new List<As>
        {
            TestData.CreateAs(number: 1, core: CoreType.None, routers: routers)
        };

        var context = ContextFactory.Create(asses: asses);
        var processor = new PopulateMtu(_logger, context);
        processor.Process();

        await Assert.That(localInterface.Mtu).IsEqualTo(9000);
    }

    [Test]
    public async Task Process_LdpIntraAsNoMtu_SetsMtuTo1524()
    {
        var localInterface = CreateLinkedInterfaces(sameAs: true);
        var routers = new List<Router>
        {
            TestData.CreateRouter(name: "R1", interfaces: [localInterface])
        };
        var asses = new List<As>
        {
            TestData.CreateAs(number: 1, core: CoreType.LDP, routers: routers)
        };

        var context = ContextFactory.Create(asses: asses);
        var processor = new PopulateMtu(_logger, context);
        processor.Process();

        await Assert.That(localInterface.Mtu).IsEqualTo(1524);
    }

    [Test]
    public async Task Process_LdpIntraAsWithCustomMtu_NoOverwrite()
    {
        var localInterface = CreateLinkedInterfaces(sameAs: true, mtu: 9000);
        var routers = new List<Router>
        {
            TestData.CreateRouter(name: "R1", interfaces: [localInterface])
        };
        var asses = new List<As>
        {
            TestData.CreateAs(number: 1, core: CoreType.LDP, routers: routers)
        };

        var context = ContextFactory.Create(asses: asses);
        var processor = new PopulateMtu(_logger, context);
        processor.Process();

        await Assert.That(localInterface.Mtu).IsEqualTo(9000);
    }

    [Test]
    public async Task Process_LdpInterAsNoMtu_SetsMtuTo1500()
    {
        var (localInterface, _) = CreateCrossAsInterfaces();
        var localRouter = TestData.CreateRouter(name: "R1", interfaces: [localInterface]);
        var remoteRouter = TestData.CreateRouter(name: "R2", interfaces: [localInterface.Neighbour!]);

        var asses = new List<As>
        {
            TestData.CreateAs(number: 1, core: CoreType.LDP, routers: [localRouter]),
            TestData.CreateAs(number: 2, core: CoreType.None, routers: [remoteRouter])
        };

        var context = ContextFactory.Create(asses: asses);
        var processor = new PopulateMtu(_logger, context);
        processor.Process();

        await Assert.That(localInterface.Mtu).IsEqualTo(1500);
    }

    [Test]
    public async Task Process_LdpInterAsWithCustomMtu_PreservesCustomMtu()
    {
        var (localInterface, _) = CreateCrossAsInterfaces(mtu: 9000);
        var localRouter = TestData.CreateRouter(name: "R1", interfaces: [localInterface]);
        var remoteRouter = TestData.CreateRouter(name: "R2", interfaces: [localInterface.Neighbour!]);

        var asses = new List<As>
        {
            TestData.CreateAs(number: 1, core: CoreType.LDP, routers: [localRouter]),
            TestData.CreateAs(number: 2, core: CoreType.None, routers: [remoteRouter])
        };

        var context = ContextFactory.Create(asses: asses);
        var processor = new PopulateMtu(_logger, context);
        processor.Process();

        await Assert.That(localInterface.Mtu).IsEqualTo(9000);
    }

    [Test]
    public async Task Process_NoLdpInterAs_SetsMtuTo1500()
    {
        var (localInterface, _) = CreateCrossAsInterfaces();
        var localRouter = TestData.CreateRouter(name: "R1", interfaces: [localInterface]);
        var remoteRouter = TestData.CreateRouter(name: "R2", interfaces: [localInterface.Neighbour!]);

        var asses = new List<As>
        {
            TestData.CreateAs(number: 1, core: CoreType.iBGP, routers: [localRouter]),
            TestData.CreateAs(number: 2, core: CoreType.None, routers: [remoteRouter])
        };

        var context = ContextFactory.Create(asses: asses);
        var processor = new PopulateMtu(_logger, context);
        processor.Process();

        await Assert.That(localInterface.Mtu).IsEqualTo(1500);
    }

    private static Interface CreateLinkedInterfaces(bool sameAs, int? mtu = null)
    {
        var remoteInterface = TestData.CreateInterface(name: "GigabitEthernet0/0");
        var localInterface = TestData.CreateInterface(
            name: "GigabitEthernet0/0",
            neighbour: remoteInterface,
            bgp: BgpRelationship.Peer);
        localInterface.Mtu = mtu;

        var remoteRouter = TestData.CreateRouter(name: "R2", interfaces: [remoteInterface]);
        var localRouter = TestData.CreateRouter(name: "R1", interfaces: [localInterface]);

        var asNumber = sameAs ? 1 : 2;
        TestData.CreateAs(number: 1, routers: [localRouter]);
        TestData.CreateAs(number: asNumber, routers: [remoteRouter]);

        return localInterface;
    }

    // ReSharper disable once UnusedTupleComponentInReturnValue
    private static (Interface Local, Interface Remote) CreateCrossAsInterfaces(int? mtu = null)
    {
        var remoteInterface = TestData.CreateInterface(name: "GigabitEthernet0/0");
        var localInterface = TestData.CreateInterface(
            name: "GigabitEthernet0/0",
            neighbour: remoteInterface,
            bgp: BgpRelationship.Peer);
        localInterface.Mtu = mtu;

        return (localInterface, remoteInterface);
    }
}