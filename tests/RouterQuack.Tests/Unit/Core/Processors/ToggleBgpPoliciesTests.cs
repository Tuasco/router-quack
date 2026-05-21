using Microsoft.Extensions.Logging;
using NSubstitute;
using RouterQuack.Core.Processors;
using RouterQuack.Tests.Unit.TestHelpers;

namespace RouterQuack.Tests.Unit.Core.Processors;

public class ToggleBgpPoliciesTests
{
    private readonly ILogger<ToggleBgpPolicies> _logger = Substitute.For<ILogger<ToggleBgpPolicies>>();

    [Test]
    public async Task Process_PlainBorderRouter_EnablesPolicies()
    {
        var remoteInterface = TestData.CreateInterface();
        var localInterface = TestData.CreateInterface(bgp: BgpRelationship.Peer, neighbour: remoteInterface);
        var router = TestData.CreateRouter(interfaces: [localInterface]);
        var ass = TestData.CreateAs(routers: [router]);

        var context = ContextFactory.Create(asses: [ass]);
        var processor = new ToggleBgpPolicies(_logger, context);
        processor.Process();

        await Assert.That(router.Bgp.Policies).IsTrue();
    }

    [Test]
    public async Task Process_VpnClient_DisablesPolicies()
    {
        var peInterface = TestData.CreateInterface(vrf: "CUSTOMER_A");
        var ceInterface = TestData.CreateInterface(bgp: BgpRelationship.Provider, neighbour: peInterface);
        var router = TestData.CreateRouter(interfaces: [ceInterface]);
        var ass = TestData.CreateAs(routers: [router]);

        var context = ContextFactory.Create(asses: [ass]);
        var processor = new ToggleBgpPolicies(_logger, context);
        processor.Process();

        await Assert.That(router.Bgp.Policies).IsFalse();
    }

    [Test]
    public async Task Process_InternalRouter_DisablesPolicies()
    {
        var router = TestData.CreateRouter();
        var ass = TestData.CreateAs(routers: [router]);

        var context = ContextFactory.Create(asses: [ass]);
        var processor = new ToggleBgpPolicies(_logger, context);
        processor.Process();

        await Assert.That(router.Bgp.Policies).IsFalse();
    }

    [Test]
    public async Task Process_AlreadyExplicitlyTrue_LeavesUnchanged()
    {
        var remoteInterface = TestData.CreateInterface();
        var localInterface = TestData.CreateInterface(bgp: BgpRelationship.Peer, neighbour: remoteInterface);
        var router = TestData.CreateRouter(
            interfaces: [localInterface],
            bgp: new() { Policies = true });
        var ass = TestData.CreateAs(routers: [router]);

        var context = ContextFactory.Create(asses: [ass]);
        var processor = new ToggleBgpPolicies(_logger, context);
        processor.Process();

        await Assert.That(router.Bgp.Policies).IsTrue();
    }

    [Test]
    public async Task Process_AlreadyExplicitlyFalse_LeavesUnchanged()
    {
        var remoteInterface = TestData.CreateInterface();
        var localInterface = TestData.CreateInterface(bgp: BgpRelationship.Peer, neighbour: remoteInterface);
        var router = TestData.CreateRouter(
            interfaces: [localInterface],
            bgp: new() { Policies = false });
        var ass = TestData.CreateAs(routers: [router]);

        var context = ContextFactory.Create(asses: [ass]);
        var processor = new ToggleBgpPolicies(_logger, context);
        processor.Process();

        await Assert.That(router.Bgp.Policies).IsFalse();
    }
}