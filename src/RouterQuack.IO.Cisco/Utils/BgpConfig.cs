using System.Net.Sockets;
using System.Text;
using RouterQuack.Core.Models;

namespace RouterQuack.IO.Cisco.Utils;

internal static class BgpConfig
{
    internal static void ApplyBgpConfig(StringBuilder builder, Router router)
    {
        // Neither eBGP nor iBGP.
        if (router is { BorderRouter: false, Bgp.Ibgp: false })
            return;

        builder.AppendLine(ConfigHeader);
        builder.AppendLine($"router bgp {router.ParentAs.Number}");
        builder.AppendLine($" bgp router-id {router.Id}");
        builder.AppendLine(ConfigStart);

        // Routers with iBGP enabled (except self)
        var ibgpNeighbours = router.ParentAs.Routers
            .Where(r => r.Bgp.Ibgp && !r.Equals(router))
            .ToArray();

        // Split eBGP interfaces: plain inter-AS links vs CE-facing (VRF-bound)
        var ebgpInterfaces = router.Interfaces
            .Where(i => i.Bgp != BgpRelationship.None)
            .ToArray();

        var plainEbgpNeighbours = ebgpInterfaces
            .Where(i => i.Vrf is null)
            .Select(i => i.Neighbour!)
            .ToArray();

        var vrfEbgpGroups = ebgpInterfaces
            .Where(i => i.Vrf is not null)
            .GroupBy(i => i.Vrf!)
            .ToArray();

        List<string> ipv4AddressFamily = [];
        List<string> ipv6AddressFamily = [];

        ConfigureEbgp(builder, plainEbgpNeighbours, ipv4AddressFamily, ipv6AddressFamily);
        ConfigureIbgp(builder, ibgpNeighbours, ipv4AddressFamily, ipv6AddressFamily);
        ConfigureNetworks(router, ipv4AddressFamily, ipv6AddressFamily);
        WriteAddressFamilies(builder, ipv4AddressFamily, ipv6AddressFamily);
        WriteVpnv4AddressFamily(builder, ibgpNeighbours, vrfEbgpGroups);
        WriteVrfAddressFamilies(builder, vrfEbgpGroups);

        builder.AppendLine("!\n!");
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
        Router[] neighbours,
        in List<string> ipv4AddressFamily,
        in List<string> ipv6AddressFamily)
    {
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

    private static void ConfigureNetworks(Router router,
        List<string> ipv4AddressFamily,
        List<string> ipv6AddressFamily)
    {
        foreach (var network in router.Bgp.Networks)
        {
            if (network.BaseAddress.AddressFamily == AddressFamily.InterNetwork)
                ipv4AddressFamily.Add($"  network {network.BaseAddress} mask " +
                                      $"{Ipv4AddressUtils.GetV4Mask(network.PrefixLength)}");
            else
                ipv6AddressFamily.Add($"  network {network.BaseAddress}/{network.PrefixLength}");
        }
    }

    private static void WriteAddressFamilies(StringBuilder builder,
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
    }

    /// <summary>
    /// Emits the vpnv4 address-family block for PE↔PE iBGP neighbours.
    /// Only emitted when the router has at least one VRF-bound eBGP interface,
    /// i.e. it is acting as a PE router.
    /// </summary>
    private static void WriteVpnv4AddressFamily(StringBuilder builder,
        Router[] ibgpNeighbours,
        IGrouping<string, Interface>[] vrfEbgpGroups)
    {
        if (vrfEbgpGroups.Length == 0 || ibgpNeighbours.Length == 0)
            return;

        builder.AppendLine(" !");
        builder.AppendLine(" address-family vpnv4");

        foreach (var neighbour in ibgpNeighbours)
        {
            var addressV4 = neighbour.LoopbackAddressV4;
            if (addressV4 is null)
                continue;

            builder.AppendLine($"  neighbor {addressV4} activate");
            builder.AppendLine($"  neighbor {addressV4} send-community extended");
        }

        builder.AppendLine("  exit-address-family");
    }

    /// <summary>
    /// Emits one "address-family ipv4 vrf NAME" block per VRF,
    /// containing the CE neighbour declarations for that VRF.
    /// </summary>
    private static void WriteVrfAddressFamilies(StringBuilder builder,
        IGrouping<string, Interface>[] vrfEbgpGroups)
    {
        if (vrfEbgpGroups.Length == 0)
            return;

        foreach (var group in vrfEbgpGroups)
        {
            builder.AppendLine(" !");
            builder.AppendLine($" address-family ipv4 vrf {group.Key}");

            foreach (var iface in group)
            {
                var neighbour = iface.Neighbour!;
                var addressV4 = neighbour.Ipv4Address?.IpAddress;
                if (addressV4 is null)
                    continue;

                builder.AppendLine($"  neighbor {addressV4} remote-as {neighbour.ParentRouter.ParentAs.Number}");
                builder.AppendLine($"  neighbor {addressV4} activate");
            }

            builder.AppendLine("  exit-address-family");
        }
    }
}
