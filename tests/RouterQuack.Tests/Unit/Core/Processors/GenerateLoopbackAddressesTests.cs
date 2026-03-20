using System.Net;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RouterQuack.Core.Processors;
using RouterQuack.Core.Utils;
using RouterQuack.Tests.Unit.TestHelpers;

namespace RouterQuack.Tests.Unit.Core.Processors;

public class GenerateLoopbackAddressesTests
{
    private readonly ILogger<GenerateLoopbackAddresses> _logger = Substitute.For<ILogger<GenerateLoopbackAddresses>>();
    private readonly NetworkUtils _networkUtils = new();

    [Test]
    public async Task Process_RouterWithoutLoopbackV4_GeneratesV4AddressOnly()
    {
        var routers = new List<Router>
        {
            TestData.CreateRouter(loopbackAddressV4: null, loopbackAddressV6: null, external: false)
        };
        var asses = new List<As>
        {
            TestData.CreateAs(
                loopbackSpaceV4: IPNetwork.Parse("10.0.0.0/24"),
                loopbackSpaceV6: IPNetwork.Parse("2001:db8::/64"),
                ipVersion: IpVersion.IPv4,
                routers: routers)
        };

        var context = ContextFactory.Create(asses: asses);
        var processor = new GenerateLoopbackAddresses(_logger, context, _networkUtils);
        processor.Process();

        await Assert.That(routers[0].LoopbackAddressV4).IsNotNull();
        await Assert.That(routers[0].LoopbackAddressV6).IsNull();
        await Assert.That(processor.Context.ErrorsOccurred).IsFalse();
    }

    [Test]
    public async Task Process_RouterWithoutLoopbackV6_GeneratesV6AddressOnly()
    {
        var routers = new List<Router>
        {
            TestData.CreateRouter(loopbackAddressV4: null, loopbackAddressV6: null, external: false)
        };
        var asses = new List<As>
        {
            TestData.CreateAs(
                loopbackSpaceV4: IPNetwork.Parse("10.0.0.0/24"),
                loopbackSpaceV6: IPNetwork.Parse("2001:db8::/64"),
                ipVersion: IpVersion.IPv6,
                routers: routers)
        };

        var context = ContextFactory.Create(asses: asses);
        var processor = new GenerateLoopbackAddresses(_logger, context, _networkUtils);
        processor.Process();

        await Assert.That(routers[0].LoopbackAddressV6).IsNotNull();
        await Assert.That(routers[0].LoopbackAddressV4).IsNull();
        await Assert.That(processor.Context.ErrorsOccurred).IsFalse();
    }

    [Test]
    [DependsOn(nameof(Process_RouterWithoutLoopbackV4_GeneratesV4AddressOnly))]
    public async Task Process_MultipleRouters_GeneratesUniqueAddressesV4()
    {
        var routers = new List<Router>
        {
            TestData.CreateRouter(name: "Router1", loopbackAddressV4: null, external: false),
            TestData.CreateRouter(name: "Router2", loopbackAddressV4: null, external: false)
        };
        var asses = new List<As>
        {
            TestData.CreateAs(
                loopbackSpaceV4: IPNetwork.Parse("10.0.0.0/24"),
                loopbackSpaceV6: IPNetwork.Parse("2001:db8::/64"),
                ipVersion: IpVersion.IPv4,
                routers: routers)
        };

        var context = ContextFactory.Create(asses: asses);
        var processor = new GenerateLoopbackAddresses(_logger, context, _networkUtils);
        processor.Process();

        await Assert.That(routers[0].LoopbackAddressV4!)
            .IsNotEqualTo(routers[1].LoopbackAddressV4!);

        await Assert.That(routers[0].LoopbackAddressV6).IsNull();
        await Assert.That(routers[1].LoopbackAddressV6).IsNull();
    }

    [Test]
    [DependsOn(nameof(Process_RouterWithoutLoopbackV6_GeneratesV6AddressOnly))]
    public async Task Process_MultipleRouters_GeneratesUniqueAddressesV6()
    {
        var routers = new List<Router>
        {
            TestData.CreateRouter(name: "Router1", loopbackAddressV6: null, external: false),
            TestData.CreateRouter(name: "Router2", loopbackAddressV6: null, external: false)
        };
        var asses = new List<As>
        {
            TestData.CreateAs(
                loopbackSpaceV4: IPNetwork.Parse("10.0.0.0/24"),
                loopbackSpaceV6: IPNetwork.Parse("2001:db8::/64"),
                ipVersion: IpVersion.IPv6,
                routers: routers)
        };

        var context = ContextFactory.Create(asses: asses);
        var processor = new GenerateLoopbackAddresses(_logger, context, _networkUtils);
        processor.Process();

        await Assert.That(routers[0].LoopbackAddressV6!)
            .IsNotEqualTo(routers[1].LoopbackAddressV6!);

        await Assert.That(routers[0].LoopbackAddressV4).IsNull();
        await Assert.That(routers[1].LoopbackAddressV4).IsNull();
    }

    [Test]
    public async Task Process_RouterWithExistingLoopbackV4_KeepsExisting()
    {
        var existingAddress = IPAddress.Parse("10.0.0.100");
        var routers = new List<Router> { TestData.CreateRouter(loopbackAddressV4: existingAddress, external: false) };
        var asses = new List<As>
        {
            TestData.CreateAs(
                loopbackSpaceV4: IPNetwork.Parse("10.0.0.0/24"),
                ipVersion: IpVersion.IPv4,
                routers: routers)
        };

        var context = ContextFactory.Create(asses: asses);
        var processor = new GenerateLoopbackAddresses(_logger, context, _networkUtils);
        processor.Process();

        await Assert.That(routers[0].LoopbackAddressV4!.ToString()).IsEqualTo("10.0.0.100");
        await Assert.That(routers[0].LoopbackAddressV6).IsNull();
    }

    [Test]
    public async Task Process_RouterWithExistingLoopbackV6_KeepsExisting()
    {
        var existingAddress = IPAddress.Parse("2001:db8::1");
        var routers = new List<Router> { TestData.CreateRouter(loopbackAddressV6: existingAddress, external: false) };
        var asses = new List<As>
        {
            TestData.CreateAs(
                loopbackSpaceV6: IPNetwork.Parse("2001:db8::/64"),
                ipVersion: IpVersion.IPv6,
                routers: routers)
        };

        var context = ContextFactory.Create(asses: asses);
        var processor = new GenerateLoopbackAddresses(_logger, context, _networkUtils);
        processor.Process();

        await Assert.That(routers[0].LoopbackAddressV6!.ToString()).IsEqualTo("2001:db8::1");
        await Assert.That(routers[0].LoopbackAddressV4).IsNull();
    }

    [Test]
    public async Task Process_ExternalRouter_NoLoopbackGenerated()
    {
        var routers = new List<Router> { TestData.CreateRouter(external: true) };
        var asses = new List<As>
        {
            TestData.CreateAs(
                loopbackSpaceV4: IPNetwork.Parse("10.0.0.0/24"),
                loopbackSpaceV6: IPNetwork.Parse("2001:db8::/64"),
                routers: routers)
        };

        var context = ContextFactory.Create(asses: asses);
        var processor = new GenerateLoopbackAddresses(_logger, context, _networkUtils);
        processor.Process();

        await Assert.That(routers[0].LoopbackAddressV4).IsNull();
        await Assert.That(routers[0].LoopbackAddressV6).IsNull();
    }
}