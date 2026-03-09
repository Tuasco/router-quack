using System.Net.Sockets;
using System.Text;
using RouterQuack.Core.Models;

namespace RouterQuack.IO.Cisco.Utils;

internal static class BgpConfig
{
    internal static void ApplyBgpConfig(StringBuilder builder, Router router)
    {
        var isBorderRouter = router.BorderRouter;

        // Neither eBGP nor iBGP.
        if (!isBorderRouter && router.ParentAs.Igp != IgpType.Ibgp)
            return;

        builder.AppendLine(ConfigHeader);
        builder.AppendLine($"router bgp {router.ParentAs.Number}");
        builder.AppendLine($" bgp router-id {router.Id}");
        builder.AppendLine(ConfigStart);

        var neighbours = router.Interfaces.Select(i => i.Neighbour!).ToArray();
        List<string> ipv4AddressFamily = [];
        List<string> ipv6AddressFamily = [];

        // Configure eBGP neighbours
        if (isBorderRouter)
            ConfigureEbgp(builder, neighbours, ipv4AddressFamily, ipv6AddressFamily);

        // Configure iBGP neighbours
        if (router.ParentAs.Igp == IgpType.Ibgp)
            ConfigureIbgp(builder, neighbours, router.ParentAs.Number, ipv4AddressFamily, ipv6AddressFamily);

        ConfigureAddressFamilies(builder, ipv4AddressFamily, ipv6AddressFamily);
    }

    private const string ConfigHeader = "! ================= BGP =================";

    private const string ConfigStart =
        """
         bgp log-neighbor-changes
         no bgp default ipv4-unicast
        """;

    private static void ConfigureEbgp(StringBuilder builder,
        Interface[] neighbours,
        in List<string> ipv4AddressFamily,
        in List<string> ipv6AddressFamily)
    {
        foreach (var neighbour in neighbours.Where(i => i.Bgp != BgpRelationship.None))
        {
            var address = neighbour.Addresses.First().IpAddress;
            builder.AppendLine($" neighbor {address} remote-as {neighbour.ParentRouter.ParentAs.Number}");

            var addressFamily = address.AddressFamily == AddressFamily.InterNetwork
                ? ipv4AddressFamily
                : ipv6AddressFamily;

            addressFamily.Add($"  neighbor {address} activate");
        }
    }

    private static void ConfigureIbgp(StringBuilder builder,
        Interface[] neighbours,
        int asNumber,
        in List<string> ipv4AddressFamily,
        in List<string> ipv6AddressFamily)
    {
        foreach (var neighbour in neighbours.Where(i => i.Bgp == BgpRelationship.None))
        {
            var address = neighbour.ParentRouter.LoopbackAddress!.IpAddress;
            builder.AppendLine($" neighbor {address} remote-as {asNumber}");
            builder.AppendLine($" neighbor {address} update-source Loopback0");

            var addressFamily = address.AddressFamily == AddressFamily.InterNetwork
                ? ipv4AddressFamily
                : ipv6AddressFamily;

            addressFamily.AddRange([$"  neighbor {address} activate", $"  neighbor {address} next-hop-self"]);
        }
    }

    private static void ConfigureAddressFamilies(StringBuilder builder,
        in List<string> ipv4AddressFamily,
        in List<string> ipv6AddressFamily)
    {
        builder.AppendLine(" !");
        builder.AppendLine(" address-family ipv4 unicast");
        builder.AppendJoin("\n", ipv4AddressFamily);
        builder.Append(ipv4AddressFamily.Any() ? "\n" : null);
        builder.AppendLine("  exit");

        builder.AppendLine(" !");
        builder.AppendLine(" address-family ipv6");
        builder.AppendJoin("\n", ipv6AddressFamily);
        builder.Append(ipv6AddressFamily.Any() ? "\n" : null);
        builder.AppendLine("  exit");

        builder.AppendLine("!\n!");
    }
}