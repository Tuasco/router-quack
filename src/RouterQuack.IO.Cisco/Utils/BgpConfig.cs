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
        ConfigureIbgp(builder, ibgpNeighbours, router, ipv4AddressFamily, ipv6AddressFamily);
        ConfigureNetworks(router, ipv4AddressFamily, ipv6AddressFamily);
        WriteAddressFamilies(builder, ipv4AddressFamily, ipv6AddressFamily);
        WriteVpnv4AddressFamily(builder, ibgpNeighbours, vrfEbgpGroups);
        WriteVrfIpv4AddressFamilies(builder, vrfEbgpGroups, router);
        WriteVrfIpv6AddressFamilies(builder, vrfEbgpGroups, router);

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
            if (neighbour.Ipv4Address is not null)
            {
                builder.AppendLine(
                    $" neighbor {neighbour.Ipv4Address.IpAddress} " + $"remote-as {neighbour.AsNumber}");
                builder.AppendLine($" neighbor {neighbour.Ipv4Address.IpAddress} send-community both");
                ipv4AddressFamily.Add($"  neighbor {neighbour.Ipv4Address.IpAddress} activate");

                ipv4AddressFamily.Add($"  neighbor {neighbour.Ipv4Address.IpAddress} " +
                                      $"route-map {BgpPolicyConfig.GetInboundRouteMapName(neighbour.Neighbour!.Bgp,
                                          neighbour.AsNumber,
                                          neighbour.ParentRouter.Name)} in");

                ipv4AddressFamily.Add($"  neighbor {neighbour.Ipv4Address.IpAddress} " +
                                      $"route-map {BgpPolicyConfig.GetOutboundRouteMapName(neighbour.Neighbour!.Bgp,
                                          neighbour.AsNumber,
                                          neighbour.ParentRouter.Name)} out");
            }

            // ReSharper disable once InvertIf
            if (neighbour.Ipv6Address is not null)
            {
                builder.AppendLine($" neighbor {neighbour.Ipv6Address.IpAddress} remote-as {neighbour.AsNumber}");
                builder.AppendLine($" neighbor {neighbour.Ipv6Address.IpAddress} send-community both");
                ipv6AddressFamily.Add($"  neighbor {neighbour.Ipv6Address.IpAddress} activate");

                ipv6AddressFamily.Add($"  neighbor {neighbour.Ipv6Address.IpAddress} " +
                                      $"route-map {BgpPolicyConfig.GetInboundRouteMapName(neighbour.Neighbour!.Bgp,
                                          neighbour.AsNumber,
                                          neighbour.ParentRouter.Name)} in");

                ipv6AddressFamily.Add($"  neighbor {neighbour.Ipv6Address.IpAddress} " +
                                      $"route-map {BgpPolicyConfig.GetOutboundRouteMapName(neighbour.Neighbour!.Bgp,
                                          neighbour.AsNumber,
                                          neighbour.ParentRouter.Name)} out");
            }
        }
    }

    private static void ConfigureIbgp(StringBuilder builder,
        Router[] neighbours,
        Router router,
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
                builder.AppendLine($" neighbor {addressV4} send-community both");
                ipv4AddressFamily.Add($"  neighbor {addressV4} activate");
                ipv4AddressFamily.Add($"  neighbor {addressV4} next-hop-self");

                // 6PE for the win
                if (router.ParentAs.Core == CoreType.LDP && router.ParentAs.AddressFamily.HasFlag(IpVersion.IPv6))
                {
                    ipv6AddressFamily.Add($"  neighbor {addressV4} activate");
                    ipv6AddressFamily.Add($"  neighbor {addressV4} next-hop-self");
                    ipv6AddressFamily.Add($"  neighbor {addressV4} send-label");
                }
            }

            var addressV6 = neighbour.LoopbackAddressV6;

            // ReSharper disable once InvertIf
            if (addressV6 is not null)
            {
                builder.AppendLine($" neighbor {addressV6} remote-as {neighbour.ParentAs.Number}");
                builder.AppendLine($" neighbor {addressV6} update-source Loopback0");
                builder.AppendLine($" neighbor {addressV6} send-community both");
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
            if (network.BaseAddress.AddressFamily == AddressFamily.InterNetwork)
                ipv4AddressFamily.Add($"  network {network.BaseAddress} " +
                                      $"mask {Ipv4AddressUtils.GetV4Mask(network.PrefixLength)} " +
                                      $"route-map {BgpPolicyConfig.SetLocalRouteMapName}");
            else
                ipv6AddressFamily.Add($"  network {network.BaseAddress}/{network.PrefixLength} " +
                                      $"route-map {BgpPolicyConfig.SetLocalRouteMapName}");
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
            builder.AppendLine($"  neighbor {addressV4} send-community both");
        }

        builder.AppendLine("  exit-address-family");
    }

    /// <summary>
    /// Emits one "address-family ipv4 vrf NAME" block per VRF,
    /// containing the CE neighbour declarations for that VRF.
    /// </summary>
    private static void WriteVrfIpv4AddressFamilies(StringBuilder builder,
        IGrouping<string, Interface>[] vrfEbgpGroups,
        Router router)
    {
        foreach (var group in vrfEbgpGroups)
        {
            // Only emit if the PE has an interface with the correct VRF
            var vrf = router.Vrfs.FirstOrDefault(v => v.Name == group.Key);
            if (vrf is null || !router.ParentAs.AddressFamily.HasFlag(IpVersion.IPv4))
                continue;

            builder.AppendLine(" !");
            builder.AppendLine($" address-family ipv4 vrf {group.Key}");

            foreach (var iface in group)
            {
                var neighbour = iface.Neighbour!;
                var addressV4 = neighbour.Ipv4Address?.IpAddress;

                if (addressV4 is null)
                    continue;

                builder.AppendLine($"  neighbor {addressV4} remote-as {neighbour.AsNumber}");
                builder.AppendLine($"  neighbor {addressV4} activate");

                // Add Community Lists
                builder.AppendLine($"  neighbor {addressV4} " +
                                   $"route-map {BgpPolicyConfig.GetInboundRouteMapName(neighbour.Neighbour!.Bgp,
                                       neighbour.AsNumber,
                                       neighbour.ParentRouter.Name)} in");

                builder.AppendLine($"  neighbor {addressV4} " +
                                   $"route-map {BgpPolicyConfig.GetOutboundRouteMapName(neighbour.Neighbour!.Bgp,
                                       neighbour.AsNumber,
                                       neighbour.ParentRouter.Name)} out");

                if (vrf.OverrideAs)
                    builder.AppendLine($"  neighbor {addressV4} as-override"); // needed for CE-to-CE same-AS scenarios
            }

            builder.AppendLine("  exit-address-family");
        }
    }

    private static void WriteVrfIpv6AddressFamilies(StringBuilder builder,
        IGrouping<string, Interface>[] vrfEbgpGroups,
        Router router)
    {
        foreach (var group in vrfEbgpGroups)
        {
            // Only emit if the PE has an interface with the correct VRF
            var vrf = router.Vrfs.FirstOrDefault(v => v.Name == group.Key);
            if (vrf is null || !router.ParentAs.AddressFamily.HasFlag(IpVersion.IPv6))
                continue;

            // Only emit if at least one neighbour actually has an IPv6 address
            var hasAnyV6Neighbour = group.Any(i => i.Neighbour?.Ipv6Address is not null);
            if (!hasAnyV6Neighbour)
                continue;

            builder.AppendLine(" !");
            builder.AppendLine($" address-family ipv6 vrf {group.Key}");

            foreach (var iface in group)
            {
                var neighbour = iface.Neighbour!;
                var addressV6 = neighbour.Ipv6Address?.IpAddress;

                if (addressV6 is null)
                    continue;

                builder.AppendLine($"  neighbor {addressV6} remote-as {neighbour.AsNumber}");
                builder.AppendLine($"  neighbor {addressV6} activate");

                // Add Community Lists
                builder.AppendLine($"  neighbor {addressV6} " +
                                   $"route-map {BgpPolicyConfig.GetInboundRouteMapName(neighbour.Neighbour!.Bgp,
                                       neighbour.AsNumber,
                                       neighbour.ParentRouter.Name)} in");

                builder.AppendLine($"  neighbor {addressV6} " +
                                   $"route-map {BgpPolicyConfig.GetOutboundRouteMapName(neighbour.Neighbour!.Bgp,
                                       neighbour.AsNumber,
                                       neighbour.ParentRouter.Name)} out");

                if (vrf.OverrideAs)
                    builder.AppendLine($"  neighbor {addressV6} as-override"); // needed for CE-to-CE same-AS scenarios
            }

            builder.AppendLine("  exit-address-family");
        }
    }
}