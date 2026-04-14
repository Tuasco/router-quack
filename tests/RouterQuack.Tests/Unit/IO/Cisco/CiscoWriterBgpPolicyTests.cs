using System.Net;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RouterQuack.IO.Cisco;
using RouterQuack.Tests.Unit.TestHelpers;

namespace RouterQuack.Tests.Unit.IO.Cisco;

public class CiscoWriterBgpPolicyTests
{
    private readonly ILogger<CiscoWriter> _logger = Substitute.For<ILogger<CiscoWriter>>();

    [Test]
    public async Task WriteFiles_EmitsPolicyHeaderAndLocalCommunityDefinitions()
    {
        var config = GenerateConfig(BgpRelationship.Client);

        await Assert.That(config).Contains("! ================= BGP Policy =================");
        await Assert.That(config).Contains("ip community-list standard RQ-SRC-LOCAL permit 65000:2000");
        await Assert.That(config).Contains("ip community-list standard RQ-SRC-CLIENT permit 65000:2100");
        await Assert.That(config).Contains("ip community-list standard RQ-SRC-PEER permit 65000:2200");
        await Assert.That(config).Contains("ip community-list standard RQ-SRC-PROVIDER permit 65000:2300");
    }

    [Test]
    public async Task WriteFiles_AttachesSetLocalRouteMapToIpv4AndIpv6Networks()
    {
        var config = GenerateConfig(BgpRelationship.Client);

        await Assert.That(config).Contains("network 203.0.113.0 mask 255.255.255.0 route-map RM-SET-LOCAL");
        await Assert.That(config).Contains("network 2001:db8:ffff::/48 route-map RM-SET-LOCAL");
        await Assert.That(config).Contains("route-map RM-SET-LOCAL permit 10");
        await Assert.That(config).Contains(" set community 65000:2000 additive");
    }

    [Test]
    public async Task WriteFiles_PeerAndProviderRelationships_EmitDifferentLocalPreferences()
    {
        var peerConfig = GenerateConfig(BgpRelationship.Peer);
        var providerConfig = GenerateConfig(BgpRelationship.Provider);

        await Assert.That(peerConfig).Contains("set local-preference 200");
        await Assert.That(providerConfig).Contains("set local-preference 100");
    }

    [Test]
    public async Task WriteFiles_InboundPolicy_ScrubsInternalCommunitiesBeforeTagging()
    {
        var config = GenerateConfig(BgpRelationship.Provider);

        await Assert.That(config).Contains("ip community-list standard CL-INTERNAL-SCRUB permit 65000:2000");
        await Assert.That(config).Contains("ip community-list standard CL-INTERNAL-SCRUB permit 65000:2100");
        await Assert.That(config).Contains("ip community-list standard CL-INTERNAL-SCRUB permit 65000:2200");
        await Assert.That(config).Contains("ip community-list standard CL-INTERNAL-SCRUB permit 65000:2300");
        await Assert.That(config).Contains("route-map RM-IN-PROVIDER-65100-EXT1 permit 10");
        await Assert.That(config).Contains(" set comm-list CL-INTERNAL-SCRUB delete");
        await Assert.That(config).Contains(" set local-preference 100");
        await Assert.That(config).Contains(" set community 65000:2300 additive");
    }

    [Test]
    public async Task WriteFiles_ClientExport_AllowsAllProvenanceClasses()
    {
        var config = GenerateConfig(BgpRelationship.Client);

        await Assert.That(config).Contains("route-map RM-OUT-CLIENT-65100-EXT1 permit 10");
        await Assert.That(config).Contains(" match community RQ-SRC-LOCAL");
        await Assert.That(config).Contains("route-map RM-OUT-CLIENT-65100-EXT1 permit 20");
        await Assert.That(config).Contains(" match community RQ-SRC-CLIENT");
        await Assert.That(config).Contains("route-map RM-OUT-CLIENT-65100-EXT1 permit 30");
        await Assert.That(config).Contains(" match community RQ-SRC-PEER");
        await Assert.That(config).Contains("route-map RM-OUT-CLIENT-65100-EXT1 permit 40");
        await Assert.That(config).Contains(" match community RQ-SRC-PROVIDER");
    }

    [Test]
    public async Task WriteFiles_PeerAndProviderExport_AllowOnlyLocalAndClient()
    {
        var peerConfig = GenerateConfig(BgpRelationship.Peer);
        var providerConfig = GenerateConfig(BgpRelationship.Provider);

        await Assert.That(peerConfig).Contains("route-map RM-OUT-PEER-65100-EXT1 permit 10");
        await Assert.That(peerConfig).Contains(" match community RQ-SRC-LOCAL");
        await Assert.That(peerConfig).Contains("route-map RM-OUT-PEER-65100-EXT1 permit 20");
        await Assert.That(peerConfig).Contains(" match community RQ-SRC-CLIENT");
        await Assert.That(peerConfig).DoesNotContain("match community RQ-SRC-PEER");
        await Assert.That(peerConfig).DoesNotContain("match community RQ-SRC-PROVIDER");

        await Assert.That(providerConfig).Contains("route-map RM-OUT-PROVIDER-65100-EXT1 permit 10");
        await Assert.That(providerConfig).Contains(" match community RQ-SRC-LOCAL");
        await Assert.That(providerConfig).Contains("route-map RM-OUT-PROVIDER-65100-EXT1 permit 20");
        await Assert.That(providerConfig).Contains(" match community RQ-SRC-CLIENT");
        await Assert.That(providerConfig).DoesNotContain("match community RQ-SRC-PEER");
        await Assert.That(providerConfig).DoesNotContain("match community RQ-SRC-PROVIDER");
    }

    [Test]
    public async Task WriteFiles_OutboundPolicy_StripsInternalCommunitiesBeforeExport()
    {
        var config = GenerateConfig(BgpRelationship.Peer);

        await Assert.That(config).Contains("ip community-list standard CL-INTERNAL-STRIP permit 65000:2000");
        await Assert.That(config).Contains("ip community-list standard CL-INTERNAL-STRIP permit 65000:2100");
        await Assert.That(config).Contains("ip community-list standard CL-INTERNAL-STRIP permit 65000:2200");
        await Assert.That(config).Contains("ip community-list standard CL-INTERNAL-STRIP permit 65000:2300");
        await Assert.That(config).Contains(" set comm-list CL-INTERNAL-STRIP delete");
    }

    [Test]
    public async Task WriteFiles_BgpNeighbourWithDualStack_AttachesPoliciesForIpv4AndIpv6()
    {
        var config = GenerateConfig(BgpRelationship.Peer);

        await Assert.That(config).Contains("neighbor 198.51.100.2 route-map RM-IN-PEER-65100-EXT1 in");
        await Assert.That(config).Contains("neighbor 198.51.100.2 route-map RM-OUT-PEER-65100-EXT1 out");
    }

    [Test]
    public async Task WriteFiles_EbgpAndIbgpNeighbours_EnableSendCommunityBoth()
    {
        var config = GenerateConfig(
            BgpRelationship.Peer,
            includeIbgpNeighbour: true);

        await Assert.That(config).Contains("neighbor 198.51.100.2 send-community both");
        await Assert.That(config).Contains("neighbor 2001:db8::2 send-community both");
        await Assert.That(config).Contains("neighbor 10.0.0.2 send-community both");
    }

    private string GenerateConfig(BgpRelationship relationship, bool includeIbgpNeighbour = false)
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"router-quack-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);

        try
        {
            var localInterface = TestData.CreateInterface(
                name: "GigabitEthernet1/0",
                bgp: relationship);

            var remoteInterface = TestData.CreateInterface(
                name: "GigabitEthernet1/0",
                bgp: GetRemoteRelationship(relationship));

            localInterface.Neighbour = remoteInterface;
            remoteInterface.Neighbour = localInterface;

            localInterface.Ipv4Address = TestData.CreateAddress("198.51.100.1", 31);
            localInterface.Ipv6Address = TestData.CreateAddress("2001:db8::1", 127);
            remoteInterface.Ipv4Address = TestData.CreateAddress("198.51.100.2", 31);
            remoteInterface.Ipv6Address = TestData.CreateAddress("2001:db8::2", 127);

            var localRouter = TestData.CreateRouter(
                name: "R1",
                id: IPAddress.Parse("1.1.1.1"),
                loopbackAddressV4: IPAddress.Parse("10.0.0.1"),
                bgp: new()
                {
                    Networks =
                    [
                        IPNetwork.Parse("203.0.113.0/24"),
                        IPNetwork.Parse("2001:db8:ffff::/48")
                    ]
                },
                interfaces: [localInterface],
                useDefaultId: false);

            if (includeIbgpNeighbour)
                localRouter.Bgp.Ibgp = true;

            var routers = new List<Router> { localRouter };

            if (includeIbgpNeighbour)
            {
                var ibgpRouter = TestData.CreateRouter(
                    name: "R2",
                    id: IPAddress.Parse("2.2.2.2"),
                    loopbackAddressV4: IPAddress.Parse("10.0.0.2"),
                    interfaces: [],
                    useDefaultId: false);
                ibgpRouter.Bgp.Ibgp = true;
                routers.Add(ibgpRouter);
            }

            var localAs = TestData.CreateAs(number: 65000, core: CoreType.iBGP, routers: routers);
            var remoteRouter = TestData.CreateRouter(
                name: "EXT1",
                id: IPAddress.Parse("3.3.3.3"),
                external: true,
                interfaces: [remoteInterface],
                useDefaultId: false);
            var remoteAs = TestData.CreateAs(number: 65100, igp: IgpType.OSPF, routers: [remoteRouter]);

            var context = ContextFactory.Create(asses: [localAs, remoteAs]);
            var writer = new CiscoWriter(_logger, context);
            writer.WriteFiles(outputDirectory);

            var configPath = Path.Combine(outputDirectory, "65000", "R1.cfg");
            return File.ReadAllText(configPath);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
                Directory.Delete(outputDirectory, true);
        }
    }

    private static BgpRelationship GetRemoteRelationship(BgpRelationship relationship)
        => relationship switch
        {
            BgpRelationship.Client => BgpRelationship.Provider,
            BgpRelationship.Provider => BgpRelationship.Client,
            _ => relationship
        };
}