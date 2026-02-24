using System.Net;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RouterQuack.Core.Processors;
using RouterQuack.Core.Utils;
using RouterQuack.Tests.Unit.TestHelpers;

namespace RouterQuack.Tests.Unit.Core.Processors;

public class GenerateLinkAddressesTests
{
    private readonly ILogger<GenerateLinkAddresses> _logger = Substitute.For<ILogger<GenerateLinkAddresses>>();
    private readonly NetworkUtils _networkUtils = new();
    private readonly InterfaceUtils _interfaceUtils = new();

    [Test]
    public async Task Process_LinkWithoutAddress_GeneratesAddresses()
    {
        var (intf1, intf2) = CreateLinkedInterfaces();
        var asses = new List<As>
        {
            TestData.CreateAs(
                networksSpaceV6: IPNetwork.Parse("2001:db8::/32"),
                networksIpVersion: IpVersion.Ipv6,
                routers:
                [
                    TestData.CreateRouter(name: "Router1", external: false, interfaces: [intf1]),
                    TestData.CreateRouter(name: "Router2", external: false, interfaces: [intf2])
                ])
        };

        var context = ContextFactory.Create(asses: asses);
        var processor = new GenerateLinkAddresses(_logger, context, _networkUtils, _interfaceUtils);
        processor.Process();

        await Assert.That(intf1.Addresses).Count().IsEqualTo(1);
        await Assert.That(intf2.Addresses).Count().IsEqualTo(1);
        await Assert.That(processor.ErrorsOccurred).IsFalse();
    }

    [Test]
    public async Task Process_ExternalRouter_SkipsGeneration()
    {
        var (intf1, intf2) = CreateLinkedInterfaces();
        var asses = new List<As>
        {
            TestData.CreateAs(
                networksSpaceV6: IPNetwork.Parse("2001:db8::/32"),
                networksIpVersion: IpVersion.Ipv6,
                routers:
                [
                    TestData.CreateRouter(name: "Router1", external: true, interfaces: [intf1]),
                    TestData.CreateRouter(name: "Router2", external: false, interfaces: [intf2])
                ])
        };

        var context = ContextFactory.Create(asses: asses);
        var processor = new GenerateLinkAddresses(_logger, context, _networkUtils, _interfaceUtils);
        processor.Process();

        await Assert.That(intf1.Addresses).Count().IsEqualTo(0);
        await Assert.That(intf2.Addresses).Count().IsEqualTo(0);
    }

    [Test]
    public async Task Process_NoNetworkSpace_ThrowsInvalidOperationException()
    {
        var (intf1, intf2) = CreateLinkedInterfaces();
        var asses = new List<As>
        {
            TestData.CreateAs(
                networksSpaceV6: null,
                networksIpVersion: IpVersion.Ipv6,
                routers:
                [
                    TestData.CreateRouter(name: "Router1", external: false, interfaces: [intf1]),
                    TestData.CreateRouter(name: "Router2", external: false, interfaces: [intf2])
                ])
        };

        var context = ContextFactory.Create(asses: asses);
        var processor = new GenerateLinkAddresses(_logger, context, _networkUtils, _interfaceUtils);

        await Assert.That(() => processor.Process())
            .Throws<InvalidOperationException>();
    }

    [Test]
    [DependsOn(nameof(Process_LinkWithoutAddress_GeneratesAddresses))]
    public async Task Process_GeneratedAddressesShareNetwork()
    {
        var (intf1, intf2) = CreateLinkedInterfaces();
        var asses = new List<As>
        {
            TestData.CreateAs(
                networksSpaceV6: IPNetwork.Parse("2001:db8::/32"),
                networksIpVersion: IpVersion.Ipv6,
                routers:
                [
                    TestData.CreateRouter(name: "Router1", external: false, interfaces: [intf1]),
                    TestData.CreateRouter(name: "Router2", external: false, interfaces: [intf2])
                ])
        };

        var context = ContextFactory.Create(asses: asses);
        var processor = new GenerateLinkAddresses(_logger, context, _networkUtils, _interfaceUtils);
        processor.Process();

        var addr1 = intf1.Addresses.First();
        var addr2 = intf2.Addresses.First();

        await Assert.That(addr1.NetworkAddress).IsEqualTo(addr2.NetworkAddress);
        await Assert.That(addr1.IpAddress).IsNotEqualTo(addr2.IpAddress);
    }

    private static (Interface, Interface) CreateLinkedInterfaces()
    {
        var interface1 = TestData.CreateInterface();
        var interface2 = TestData.CreateInterface();

        interface1.Neighbour = interface2;
        interface2.Neighbour = interface1;

        return (interface1, interface2);
    }
}