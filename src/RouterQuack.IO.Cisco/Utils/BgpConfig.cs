using System.Text;
using RouterQuack.Core.Models;

namespace RouterQuack.IO.Cisco.Utils;

internal static class BgpConfig
{
    internal static void ApplyBgpConfig(StringBuilder builder, Router router)
    {
        var isBorderRouter = router.BorderRouter;

        // Neither eBGP nor iBGP.
        if (!isBorderRouter && router.ParentAs.Igp != IgpType.iBGP)
            return;

        builder.AppendLine(ConfigHeader);
        builder.AppendLine($"router bgp {router.ParentAs.Number}");
        builder.AppendLine($" bgp router-id {router.Id}");
        builder.AppendLine(ConfigStart);

        var ibgpNeighbours = router.ParentAs.Routers
            .Where(r => !r.Equals(router))
            .ToArray();

        var ebgpNeighbours = router.Interfaces
            .Where(i => i.Bgp != BgpRelationship.None && i.Bgp != BgpRelationship.Internal)
            .Select(i => i.Neighbour!)
            .ToArray();

        List<string> ipv4AddressFamily = [];
        List<string> ipv6AddressFamily = [];

        ConfigureEbgp(builder, ebgpNeighbours, ipv4AddressFamily, ipv6AddressFamily);
        ConfigureIbgp(builder, router.ParentAs.Igp, ibgpNeighbours, ipv4AddressFamily, ipv6AddressFamily);
        ConfigureAddressFamilies(builder, ipv4AddressFamily, ipv6AddressFamily);
    }

    private const string ConfigHeader = "! ================= BGP =================";

    private const string ConfigStart =
        """
         bgp log-neighbor-changes
         bgp graceful-restart
         no bgp default ipv4-unicast
        """;

    private static void ConfigureEbgp(StringBuilder builder,
        Interface[] neighbours,
        in List<string> ipv4AddressFamily,
        in List<string> ipv6AddressFamily)
    {
        foreach (var neighbour in neighbours)
        {
            var addressV4 = neighbour.Ipv4Address?.IpAddress;

            if (addressV4 is not null)
            {
                builder.AppendLine($" neighbor {addressV4} remote-as {neighbour.ParentRouter.ParentAs.Number}");
                ipv4AddressFamily.Add($"  neighbor {addressV4} activate");
            }

            var addressV6 = neighbour.Ipv6Address?.IpAddress;

            // ReSharper disable once InvertIf
            if (addressV6 is not null)
            {
                builder.AppendLine($" neighbor {addressV6} remote-as {neighbour.ParentRouter.ParentAs.Number}");
                ipv6AddressFamily.Add($"  neighbor {addressV6} activate");
            }
        }
    }

    private static void ConfigureIbgp(StringBuilder builder,
        IgpType igp,
        Router[] neighbours,
        in List<string> ipv4AddressFamily,
        in List<string> ipv6AddressFamily)
    {
        // Only configure all routers in the core if the igp is iBGP
        if (igp != IgpType.iBGP)
            neighbours = neighbours.Where(n => n.BorderRouter).ToArray();

        foreach (var neighbour in neighbours)
        {
            var addressV4 = neighbour.LoopbackAddressV4;

            if (addressV4 is not null)
            {
                builder.AppendLine($" neighbor {addressV4} remote-as {neighbour.ParentAs.Number}");
                builder.AppendLine($" neighbor {addressV4} update-source Loopback0");
                ipv4AddressFamily.Add($"  neighbor {addressV4} activate");
                ipv4AddressFamily.Add($"  neighbor {addressV4} next-hop-self");
            }

            var addressV6 = neighbour.LoopbackAddressV6;

            // ReSharper disable once InvertIf
            if (addressV6 is not null)
            {
                builder.AppendLine($" neighbor {addressV6} remote-as {neighbour.ParentAs.Number}");
                builder.AppendLine($" neighbor {addressV6} update-source Loopback0");
                ipv6AddressFamily.Add($"  neighbor {addressV6} activate");
                ipv6AddressFamily.Add($"  neighbor {addressV6} next-hop-self");
            }
        }
    }

    private static void ConfigureAddressFamilies(StringBuilder builder,
        in List<string> ipv4AddressFamily,
        in List<string> ipv6AddressFamily)
    {
        builder.AppendLine(" !");
        builder.AppendLine(" address-family ipv4 unicast");
        builder.AppendJoin("\n", ipv4AddressFamily);
        builder.AppendLine($"{(ipv4AddressFamily.Any() ? '\n' : null)}  exit-address-family");

        builder.AppendLine(" !");
        builder.AppendLine(" address-family ipv6");
        builder.AppendJoin("\n", ipv6AddressFamily);
        builder.AppendLine($"{(ipv6AddressFamily.Any() ? '\n' : null)}  exit-address-family");

        builder.AppendLine("!\n!");
    }
}