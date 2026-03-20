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

    [Test]
    public async Task Process_LinkWithoutAddress_GeneratesV4AddressOnly()
    {
        var (intf1, intf2) = CreateLinkedInterfaces();
        var asses = new List<As>
        {
            TestData.CreateAs(
                networksSpaceV4: IPNetwork.Parse("192.168.1.0/24"),
                networksSpaceV6: IPNetwork.Parse("2001:db8::/64"),
                ipVersion: IpVersion.IPv4,
                routers:
                [
                    TestData.CreateRouter(name: "Router1", external: false, interfaces: [intf1]),
                    TestData.CreateRouter(name: "Router2", external: false, interfaces: [intf2])
                ])
        };

        var context = ContextFactory.Create(asses: asses);
        var processor = new GenerateLinkAddresses(_logger, context, _networkUtils);
        processor.Process();

        await Assert.That(intf1.Ipv4Address).IsNotNull();
        await Assert.That(intf2.Ipv4Address).IsNotNull();
        await Assert.That(intf1.Ipv4Address!.NetworkAddress).IsEqualTo(intf2.Ipv4Address!.NetworkAddress);
        await Assert.That(intf1.Ipv4Address!.IpAddress).IsNotEqualTo(intf2.Ipv4Address!.IpAddress);
        await Assert.That(intf1.Ipv6Address).IsNull();
        await Assert.That(intf2.Ipv6Address).IsNull();
        await Assert.That(intf1.Addresses.Count).EqualTo(0);
        await Assert.That(intf2.Addresses.Count).EqualTo(0);
        await Assert.That(processor.Context.ErrorsOccurred).IsFalse();
    }

    [Test]
    public async Task Process_LinkWithoutAddress_GeneratesV6AddressOnly()
    {
        var (intf1, intf2) = CreateLinkedInterfaces();
        var asses = new List<As>
        {
            TestData.CreateAs(
                networksSpaceV4: IPNetwork.Parse("192.168.1.0/24"),
                networksSpaceV6: IPNetwork.Parse("2001:db8::/64"),
                ipVersion: IpVersion.IPv6,
                routers:
                [
                    TestData.CreateRouter(name: "Router1", external: false, interfaces: [intf1]),
                    TestData.CreateRouter(name: "Router2", external: false, interfaces: [intf2])
                ])
        };

        var context = ContextFactory.Create(asses: asses);
        var processor = new GenerateLinkAddresses(_logger, context, _networkUtils);
        processor.Process();

        await Assert.That(intf1.Ipv6Address).IsNotNull();
        await Assert.That(intf2.Ipv6Address).IsNotNull();
        await Assert.That(intf1.Ipv6Address!.NetworkAddress).IsEqualTo(intf2.Ipv6Address!.NetworkAddress);
        await Assert.That(intf1.Ipv6Address!.IpAddress).IsNotEqualTo(intf2.Ipv6Address!.IpAddress);
        await Assert.That(intf1.Ipv4Address).IsNull();
        await Assert.That(intf2.Ipv4Address).IsNull();
        await Assert.That(intf1.Addresses.Count).EqualTo(0);
        await Assert.That(intf2.Addresses.Count).EqualTo(0);
        await Assert.That(processor.Context.ErrorsOccurred).IsFalse();
    }

    [Test]
    public async Task Process_ExternalRouterWithValidIps_SkipsGeneration()
    {
        var (intf1, intf2) = CreateLinkedInterfaces(withValidLinkAddresses: true);

        var asses = new List<As>
        {
            TestData.CreateAs(
                networksSpaceV4: IPNetwork.Parse("192.168.0.0/16"),
                networksSpaceV6: IPNetwork.Parse("2001:db8::/32"),
                ipVersion: IpVersion.IPv4 | IpVersion.IPv6,
                routers:
                [
                    TestData.CreateRouter(name: "Router1", external: true, interfaces: [intf1]),
                    TestData.CreateRouter(name: "Router2", external: false, interfaces: [intf2])
                ])
        };

        var context = ContextFactory.Create(asses: asses);
        var processor = new GenerateLinkAddresses(_logger, context, _networkUtils);
        processor.Process();

        await Assert.That(intf1.Addresses).Count().IsEqualTo(0);
        await Assert.That(intf2.Addresses).Count().IsEqualTo(0);
        await Assert.That(processor.Context.ErrorsOccurred).IsFalse();
    }

    [Test]
    public async Task Process_ExternalRouterWithoutIps_SetsErrorsOccurred()
    {
        var (intf1, intf2) = CreateLinkedInterfaces();

        var asses = new List<As>
        {
            TestData.CreateAs(
                networksSpaceV6: IPNetwork.Parse("2001:db8::/32"),
                networksSpaceV4: IPNetwork.Parse("10.10.0.0/16"),
                ipVersion: IpVersion.IPv4 | IpVersion.IPv6,
                routers:
                [
                    TestData.CreateRouter(name: "Router1", external: true, interfaces: [intf1]),
                    TestData.CreateRouter(name: "Router2", external: false, interfaces: [intf2])
                ])
        };

        var context = ContextFactory.Create(asses: asses);
        var processor = new GenerateLinkAddresses(_logger, context, _networkUtils);
        processor.Process();

        await Assert.That(processor.Context.ErrorsOccurred).IsTrue();
        await Assert.That(intf1.Addresses).Count().IsEqualTo(0);
        await Assert.That(intf2.Addresses).Count().IsEqualTo(0);
        await Assert.That(intf1.Ipv4Address).IsNull()
            .And.EqualTo(intf1.Ipv6Address)
            .And.EqualTo(intf2.Ipv4Address)
            .And.EqualTo(intf1.Ipv6Address);
    }

    [Test]
    [Arguments(IpVersion.IPv4, "192.168.1.0/31", "192.168.1.1/30")]
    [Arguments(IpVersion.IPv4, "192.168.1.0/31", "192.168.2.0/31")]
    [Arguments(IpVersion.IPv4, "192.168.1.0/31", "192.168.1.2/31")]
    [Arguments(IpVersion.IPv6, "2001:db8::/127", "2001:db8::/126")]
    [Arguments(IpVersion.IPv6, "2001:db8:1::/127", "2001:db8:2::/127")]
    [Arguments(IpVersion.IPv6, "2001:db8:1::/127", "2001:db8:1::2/127")]
    public async Task Process_ExternalRouterWithInvalidIps_SetsErrorsOccurred(IpVersion addressFamilies,
        string address1,
        string address2)
    {
        var (intf1, intf2) = CreateLinkedInterfaces();

        intf1.Addresses.Add(new(address1));
        intf2.Addresses.Add(new(address2));

        var asses = new List<As>
        {
            TestData.CreateAs(
                ipVersion: addressFamilies,
                routers:
                [
                    TestData.CreateRouter(name: "Router1", external: true, interfaces: [intf1]),
                    TestData.CreateRouter(name: "Router2", external: false, interfaces: [intf2])
                ])
        };

        var context = ContextFactory.Create(asses: asses);
        var processor = new GenerateLinkAddresses(_logger, context, _networkUtils);
        processor.Process();

        await Assert.That(intf1.Addresses).Count().IsEqualTo(1);
        await Assert.That(intf2.Addresses).Count().IsEqualTo(1);
        await Assert.That(processor.Context.ErrorsOccurred).IsTrue();
        await Assert.That(intf1.Ipv4Address).IsNull()
            .And.EqualTo(intf1.Ipv6Address)
            .And.IsEqualTo(intf2.Ipv4Address)
            .And.IsEqualTo(intf2.Ipv6Address);
    }

    [Test]
    [Arguments(IpVersion.IPv4)]
    [Arguments(IpVersion.IPv6)]
    [Arguments(IpVersion.IPv4 | IpVersion.IPv6)]
    public async Task Process_NoNetworkSpace_SetsErrorsOccurred(IpVersion addressFamilies)
    {
        var (intf1, intf2) = CreateLinkedInterfaces();
        var asses = new List<As>
        {
            TestData.CreateAs(
                networksSpaceV6: null,
                ipVersion: addressFamilies,
                routers:
                [
                    TestData.CreateRouter(name: "Router1", external: false, interfaces: [intf1]),
                    TestData.CreateRouter(name: "Router2", external: false, interfaces: [intf2])
                ])
        };

        var context = ContextFactory.Create(asses: asses);
        var processor = new GenerateLinkAddresses(_logger, context, _networkUtils);
        processor.Process();

        await Assert.That(processor.Context.ErrorsOccurred).IsTrue();
    }

    private static (Interface, Interface) CreateLinkedInterfaces(
        bool withValidLinkAddresses = false)
    {
        var interface1 = TestData.CreateInterface();
        var interface2 = TestData.CreateInterface();

        interface1.Neighbour = interface2;
        interface2.Neighbour = interface1;

        if (withValidLinkAddresses)
        {
            var networkV4 = IPNetwork.Parse("192.168.1.0/31");
            interface1.Addresses.Add(new(networkV4, IPAddress.Parse("192.168.1.0")));
            interface2.Addresses.Add(new(networkV4, IPAddress.Parse("192.168.1.1")));

            var networkV6 = IPNetwork.Parse("2001:db8::/127");
            interface1.Addresses.Add(new(networkV6, IPAddress.Parse("2001:db8::")));
            interface2.Addresses.Add(new(networkV6, IPAddress.Parse("2001:db8::1")));
        }

        return (interface1, interface2);
    }
}