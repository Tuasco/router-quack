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
    public async Task Process_LinkWithoutAddress_GeneratesAddressesV4()
    {
        var (intf1, intf2) = CreateLinkedInterfaces();
        var asses = new List<As>
        {
            TestData.CreateAs(
                networksSpaceV4: IPNetwork.Parse("192.168.1.0/24"),
                networksIpVersion: IpVersion.Ipv4,
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
        await Assert.That(processor.Context.ErrorsOccurred).IsFalse();
    }

    [Test]
    public async Task Process_LinkWithoutAddress_GeneratesAddressesV6()
    {
        var (intf1, intf2) = CreateLinkedInterfaces();
        var asses = new List<As>
        {
            TestData.CreateAs(
                networksSpaceV6: IPNetwork.Parse("2001:db8::/64"),
                networksIpVersion: IpVersion.Ipv6,
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
        await Assert.That(processor.Context.ErrorsOccurred).IsFalse();
    }

    [Test]
    public async Task Process_ExternalRouterWithValidIps_SkipsGeneration()
    {
        var (intf1, intf2) = CreateLinkedInterfaces(withValidLinkAddresses: true);

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
        var processor = new GenerateLinkAddresses(_logger, context, _networkUtils);
        processor.Process();

        await Assert.That(intf1.Addresses).Count().IsEqualTo(1);
        await Assert.That(intf2.Addresses).Count().IsEqualTo(1);
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
                networksIpVersion: IpVersion.Ipv6,
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
        await Assert.That(processor.Context.ErrorsOccurred).IsTrue();
    }

    [Test]
    [Arguments("2001:db8::/127", "2001:db8::/126")]
    [Arguments("2001:db8:1::/127", "2001:db8:2::/127")]
    public async Task Process_ExternalRouterWithInvalidIps_SetsErrorsOccurred(string address1, string address2)
    {
        var (intf1, intf2) = CreateLinkedInterfaces();

        intf1.Addresses.Add(new(address1));
        intf2.Addresses.Add(new(address2));

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
        var processor = new GenerateLinkAddresses(_logger, context, _networkUtils);
        processor.Process();

        await Assert.That(intf1.Addresses).Count().IsEqualTo(1);
        await Assert.That(intf2.Addresses).Count().IsEqualTo(1);
        await Assert.That(processor.Context.ErrorsOccurred).IsTrue();
    }

    [Test]
    public async Task Process_NoNetworkSpace_SetsErrorsOccurred()
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
        var processor = new GenerateLinkAddresses(_logger, context, _networkUtils);
        processor.Process();

        await Assert.That(processor.Context.ErrorsOccurred).IsTrue();
    }

    private static (Interface, Interface) CreateLinkedInterfaces(bool withValidLinkAddresses = false)
    {
        var interface1 = TestData.CreateInterface();
        var interface2 = TestData.CreateInterface();

        interface1.Neighbour = interface2;
        interface2.Neighbour = interface1;

        // ReSharper disable once InvertIf
        if (withValidLinkAddresses)
        {
            var network = IPNetwork.Parse("2001:db8::/127");
            interface1.Addresses.Add(new(network, IPAddress.Parse("2001:db8::")));
            interface2.Addresses.Add(new(network, IPAddress.Parse("2001:db8::1")));
        }

        return (interface1, interface2);
    }
}