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
        var localInterface = TestData.CreateInterface(bgp: BgpRelationship.Peer);
        var remoteInterface = TestData.CreateInterface();
        TestData.LinkInterfaces(localInterface, remoteInterface);

        var localRouter = TestData.CreateRouter(name: "R1", interfaces: [localInterface]);
        var remoteRouter = TestData.CreateRouter(name: "R2", interfaces: [remoteInterface]);
        TestData.CreateAs(number: 1, routers: [remoteRouter]);

        var asses = new List<As>
        {
            TestData.CreateAs(number: 1, core: CoreType.None, routers: [localRouter])
        };

        var context = ContextFactory.Create(asses: asses);
        var processor = new PopulateMtu(_logger, context);
        processor.Process();

        await Assert.That(localInterface.Mtu).IsEqualTo(1500);
    }

    [Test]
    public async Task Process_NoLdpWithCustomMtu_NoOverwrite()
    {
        var localInterface = TestData.CreateInterface(bgp: BgpRelationship.Peer);
        var remoteInterface = TestData.CreateInterface();
        TestData.LinkInterfaces(localInterface, remoteInterface);
        localInterface.Mtu = 9000;

        var localRouter = TestData.CreateRouter(name: "R1", interfaces: [localInterface]);
        var remoteRouter = TestData.CreateRouter(name: "R2", interfaces: [remoteInterface]);
        TestData.CreateAs(number: 1, routers: [remoteRouter]);

        var asses = new List<As>
        {
            TestData.CreateAs(number: 1, core: CoreType.None, routers: [localRouter])
        };

        var context = ContextFactory.Create(asses: asses);
        var processor = new PopulateMtu(_logger, context);
        processor.Process();

        await Assert.That(localInterface.Mtu).IsEqualTo(9000);
    }

    [Test]
    public async Task Process_LdpIntraAsNoMtu_SetsMtuTo1524()
    {
        var localInterface = TestData.CreateInterface(bgp: BgpRelationship.Peer);
        var remoteInterface = TestData.CreateInterface();
        TestData.LinkInterfaces(localInterface, remoteInterface);

        var localRouter = TestData.CreateRouter(name: "R1", interfaces: [localInterface]);
        var remoteRouter = TestData.CreateRouter(name: "R2", interfaces: [remoteInterface]);
        TestData.CreateAs(number: 1, routers: [remoteRouter]);

        var asses = new List<As>
        {
            TestData.CreateAs(number: 1, core: CoreType.LDP, routers: [localRouter])
        };

        var context = ContextFactory.Create(asses: asses);
        var processor = new PopulateMtu(_logger, context);
        processor.Process();

        await Assert.That(localInterface.Mtu).IsEqualTo(1524);
    }

    [Test]
    public async Task Process_LdpIntraAsWithCustomMtu_NoOverwrite()
    {
        var localInterface = TestData.CreateInterface(bgp: BgpRelationship.Peer);
        var remoteInterface = TestData.CreateInterface();
        TestData.LinkInterfaces(localInterface, remoteInterface);
        localInterface.Mtu = 9000;

        var localRouter = TestData.CreateRouter(name: "R1", interfaces: [localInterface]);
        var remoteRouter = TestData.CreateRouter(name: "R2", interfaces: [remoteInterface]);
        TestData.CreateAs(number: 1, routers: [remoteRouter]);

        var asses = new List<As>
        {
            TestData.CreateAs(number: 1, core: CoreType.LDP, routers: [localRouter])
        };

        var context = ContextFactory.Create(asses: asses);
        var processor = new PopulateMtu(_logger, context);
        processor.Process();

        await Assert.That(localInterface.Mtu).IsEqualTo(9000);
    }

    [Test]
    public async Task Process_LdpInterAsNoMtu_SetsMtuTo1500()
    {
        var localInterface = TestData.CreateInterface(bgp: BgpRelationship.Peer);
        var remoteInterface = TestData.CreateInterface();
        TestData.LinkInterfaces(localInterface, remoteInterface);

        var localRouter = TestData.CreateRouter(name: "R1", interfaces: [localInterface]);
        var remoteRouter = TestData.CreateRouter(name: "R2", interfaces: [remoteInterface]);

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
        var localInterface = TestData.CreateInterface(bgp: BgpRelationship.Peer);
        var remoteInterface = TestData.CreateInterface();
        TestData.LinkInterfaces(localInterface, remoteInterface);
        localInterface.Mtu = 9000;

        var localRouter = TestData.CreateRouter(name: "R1", interfaces: [localInterface]);
        var remoteRouter = TestData.CreateRouter(name: "R2", interfaces: [remoteInterface]);

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
        var localInterface = TestData.CreateInterface(bgp: BgpRelationship.Peer);
        var remoteInterface = TestData.CreateInterface();
        TestData.LinkInterfaces(localInterface, remoteInterface);

        var localRouter = TestData.CreateRouter(name: "R1", interfaces: [localInterface]);
        var remoteRouter = TestData.CreateRouter(name: "R2", interfaces: [remoteInterface]);

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
}