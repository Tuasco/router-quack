using System.Net;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RouterQuack.IO.Cisco;
using RouterQuack.Tests.Unit.TestHelpers;

namespace RouterQuack.Tests.Unit.IO.Cisco;

public class CiscoWriterBgpVrfTests
{
    private readonly ILogger<CiscoWriter> _logger = Substitute.For<ILogger<CiscoWriter>>();

    [Test]
    public async Task WriteFiles_VrfEbgpWithIbgp_EmitsVpnv4AddressFamily()
    {
        var config = GenerateVrfBgpConfig();

        await Assert.That(config).Contains(" address-family vpnv4");
        await Assert.That(config).Contains(" neighbor 10.0.0.2 activate");
        await Assert.That(config).Contains(" neighbor 10.0.0.2 send-community both");
    }

    [Test]
    public async Task WriteFiles_VrfEbgpWithIbgpAndIpv6_EmitsVpnv6AddressFamily()
    {
        var config = GenerateVrfBgpConfig(ipVersion: IpVersion.IPv6 | IpVersion.IPv4);

        await Assert.That(config).Contains(" address-family vpnv6");
        await Assert.That(config).Contains(" neighbor 10.0.0.2 activate");
        await Assert.That(config).Contains(" neighbor 10.0.0.2 send-community both");
    }

    [Test]
    public async Task WriteFiles_VrfEbgpWithoutIbgp_DoesNotEmitVpnvAddressFamilies()
    {
        var config = GenerateVrfBgpConfig(includeIbgpNeighbour: false);

        await Assert.That(config).DoesNotContain(" address-family vpnv4");
        await Assert.That(config).DoesNotContain(" address-family vpnv6");
    }

    [Test]
    public async Task WriteFiles_VrfEbgp_EmitsVrfIpv4AddressFamily()
    {
        var config = GenerateVrfBgpConfig();

        await Assert.That(config).Contains(" address-family ipv4 vrf CUSTOMER_A");
        await Assert.That(config).Contains(" neighbor 192.168.1.2 remote-as 65100");
        await Assert.That(config).Contains(" neighbor 192.168.1.2 activate");
    }

    [Test]
    public async Task WriteFiles_VrfEbgpWithIpv6_EmitsVrfIpv6AddressFamily()
    {
        var config = GenerateVrfBgpConfig(ipVersion: IpVersion.IPv6 | IpVersion.IPv4, includeIpv6: true);

        await Assert.That(config).Contains(" address-family ipv6 vrf CUSTOMER_A");
        await Assert.That(config).Contains(" neighbor 2001:db8::2 remote-as 65100");
        await Assert.That(config).Contains(" neighbor 2001:db8::2 activate");
    }

    [Test]
    public async Task WriteFiles_VrfEbgpWithoutIpv6Neighbour_DoesNotEmitVrfIpv6AddressFamily()
    {
        var config = GenerateVrfBgpConfig(ipVersion: IpVersion.IPv6 | IpVersion.IPv4, includeIpv6: false);

        await Assert.That(config).DoesNotContain(" address-family ipv6 vrf CUSTOMER_A");
    }

    [Test]
    public async Task WriteFiles_VrfEbgpWithOverrideAs_EmitsAsOverride()
    {
        var config = GenerateVrfBgpConfig(overrideAs: true);

        await Assert.That(config).Contains(" neighbor 192.168.1.2 as-override");
    }

    [Test]
    public async Task WriteFiles_VrfEbgpWithoutOverrideAs_DoesNotEmitAsOverride()
    {
        var config = GenerateVrfBgpConfig(overrideAs: false);

        await Assert.That(config).DoesNotContain(" as-override");
    }

    [Test]
    public async Task WriteFiles_VrfEbgp_DoesNotEmitRouteMapsForVrfNeighbors()
    {
        var config = GenerateVrfBgpConfig();

        await Assert.That(config).DoesNotContain(" neighbor 192.168.1.2 route-map");
    }

    [Test]
    public async Task WriteFiles_MultipleVrfs_EmitsSeparateAddressFamilies()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"router-quack-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            // Create VRFs
            var vrfA = new Vrf
            {
                Name = "CUSTOMER_A",
                RouteDistinguisher = "111:1",
                ImportTargets = ["111:100"],
                ExportTargets = ["111:100"]
            };

            var vrfB = new Vrf
            {
                Name = "CUSTOMER_B",
                RouteDistinguisher = "111:2",
                ImportTargets = ["111:200"],
                ExportTargets = ["111:200"]
            };

            // Create interfaces for both VRFs
            var interfaceA = TestData.CreateInterface(
                name: "GigabitEthernet1/0",
                bgp: BgpRelationship.Client,
                vrf: "CUSTOMER_A");
            var interfaceB = TestData.CreateInterface(
                name: "GigabitEthernet2/0",
                bgp: BgpRelationship.Client,
                vrf: "CUSTOMER_B");

            var ceInterfaceA = TestData.CreateInterface(
                name: "GigabitEthernet1/0",
                bgp: BgpRelationship.Provider);
            var ceInterfaceB = TestData.CreateInterface(
                name: "GigabitEthernet1/0",
                bgp: BgpRelationship.Provider);

            interfaceA.Neighbour = ceInterfaceA;
            ceInterfaceA.Neighbour = interfaceA;
            interfaceB.Neighbour = ceInterfaceB;
            ceInterfaceB.Neighbour = interfaceB;

            interfaceA.Ipv4Address = TestData.CreateAddress("192.168.1.1", 24);
            ceInterfaceA.Ipv4Address = TestData.CreateAddress("192.168.1.2", 24);
            interfaceB.Ipv4Address = TestData.CreateAddress("192.168.2.1", 24);
            ceInterfaceB.Ipv4Address = TestData.CreateAddress("192.168.2.2", 24);

            // Create iBGP neighbour
            var ibgpInterface = TestData.CreateInterface(name: "Loopback0");
            var ibgpRouter = TestData.CreateRouter(
                name: "R2",
                id: IPAddress.Parse("2.2.2.2"),
                loopbackAddressV4: IPAddress.Parse("10.0.0.2"),
                interfaces: [ibgpInterface],
                useDefaultId: false);
            ibgpRouter.Bgp.Ibgp = true;

            var peRouter = TestData.CreateRouter(
                name: "PE1",
                id: IPAddress.Parse("1.1.1.1"),
                loopbackAddressV4: IPAddress.Parse("10.0.0.1"),
                interfaces: [interfaceA, interfaceB],
                vrfs: [vrfA, vrfB],
                useDefaultId: false);
            peRouter.Bgp.Ibgp = true;

            var localAs = TestData.CreateAs(
                number: 111,
                routers: [peRouter, ibgpRouter]);

            var ceRouterA = TestData.CreateRouter(
                name: "CE-A",
                id: IPAddress.Parse("3.3.3.3"),
                external: true,
                interfaces: [ceInterfaceA],
                useDefaultId: false);
            var ceRouterB = TestData.CreateRouter(
                name: "CE-B",
                id: IPAddress.Parse("4.4.4.4"),
                external: true,
                interfaces: [ceInterfaceB],
                useDefaultId: false);
            var ceAs = TestData.CreateAs(number: 65100, routers: [ceRouterA, ceRouterB]);

            var context = ContextFactory.Create(asses: [localAs, ceAs]);
            var writer = new CiscoWriter(_logger, context);
            writer.WriteFiles(outputDirectory);

            var configPath = Path.Combine(outputDirectory, "111", "PE1.cfg");
            var config = await File.ReadAllTextAsync(configPath);

            // Both VRF address families should be present
            await Assert.That(config).Contains(" address-family ipv4 vrf CUSTOMER_A");
            await Assert.That(config).Contains(" address-family ipv4 vrf CUSTOMER_B");

            // Each should have its own neighbour
            await Assert.That(config).Contains(" neighbor 192.168.1.2 remote-as 65100");
            await Assert.That(config).Contains(" neighbor 192.168.2.2 remote-as 65100");
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
                Directory.Delete(outputDirectory, true);
        }
    }

    private string GenerateVrfBgpConfig(
        bool includeIbgpNeighbour = true,
        IpVersion ipVersion = IpVersion.IPv4,
        bool includeIpv6 = false,
        bool overrideAs = true)
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"router-quack-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            // Create VRF with OverrideAs setting
            var vrf = new Vrf
            {
                Name = "CUSTOMER_A",
                RouteDistinguisher = "111:1",
                ImportTargets = ["111:100"],
                ExportTargets = ["111:100"],
                OverrideAs = overrideAs
            };

            // Create VRF-bound interface
            var peInterface = TestData.CreateInterface(
                name: "GigabitEthernet1/0",
                bgp: BgpRelationship.Client,
                vrf: "CUSTOMER_A");
            var ceInterface = TestData.CreateInterface(
                name: "GigabitEthernet1/0",
                bgp: BgpRelationship.Provider);

            peInterface.Neighbour = ceInterface;
            ceInterface.Neighbour = peInterface;

            peInterface.Ipv4Address = TestData.CreateAddress("192.168.1.1", 24);
            ceInterface.Ipv4Address = TestData.CreateAddress("192.168.1.2", 24);

            if (includeIpv6)
            {
                peInterface.Ipv6Address = TestData.CreateAddress("2001:db8::1", 64);
                ceInterface.Ipv6Address = TestData.CreateAddress("2001:db8::2", 64);
            }

            // Create router list
            var routers = new List<Router>();

            var peRouter = TestData.CreateRouter(
                name: "PE1",
                id: IPAddress.Parse("1.1.1.1"),
                loopbackAddressV4: IPAddress.Parse("10.0.0.1"),
                interfaces: [peInterface],
                vrfs: [vrf],
                useDefaultId: false);
            peRouter.Bgp.Ibgp = includeIbgpNeighbour;
            routers.Add(peRouter);

            if (includeIbgpNeighbour)
            {
                var ibgpInterface = TestData.CreateInterface(name: "Loopback0");
                var ibgpRouter = TestData.CreateRouter(
                    name: "R2",
                    id: IPAddress.Parse("2.2.2.2"),
                    loopbackAddressV4: IPAddress.Parse("10.0.0.2"),
                    interfaces: [ibgpInterface],
                    useDefaultId: false);
                ibgpRouter.Bgp.Ibgp = true;
                routers.Add(ibgpRouter);
            }

            var localAs = TestData.CreateAs(
                number: 111,
                ipVersion: ipVersion,
                routers: routers);

            var ceRouter = TestData.CreateRouter(
                name: "CE1",
                id: IPAddress.Parse("3.3.3.3"),
                external: true,
                interfaces: [ceInterface],
                useDefaultId: false);
            var ceAs = TestData.CreateAs(number: 65100, routers: [ceRouter]);

            var context = ContextFactory.Create(asses: [localAs, ceAs]);
            var writer = new CiscoWriter(_logger, context);
            writer.WriteFiles(outputDirectory);

            var configPath = Path.Combine(outputDirectory, "111", "PE1.cfg");
            return File.ReadAllText(configPath);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
                Directory.Delete(outputDirectory, true);
        }
    }
}