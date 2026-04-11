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

        // Neighbour interfaces with BGP relationships other than none
        var ebgpNeighbours = router.Interfaces
            .Where(i => i.Bgp != BgpRelationship.None)
            .Select(i => i.Neighbour!)
            .ToArray();

        List<string> ipv4AddressFamily = [];
        List<string> ipv6AddressFamily = [];

        ConfigureEbgp(builder, ebgpNeighbours, ipv4AddressFamily, ipv6AddressFamily);
        ConfigureIbgp(builder, ibgpNeighbours, ipv4AddressFamily, ipv6AddressFamily);
        ConfigureNetworks(router, ipv4AddressFamily, ipv6AddressFamily);
        WriteAddressFamilies(builder, ipv4AddressFamily, ipv6AddressFamily);
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
                    $" neighbor {neighbour.Ipv4Address.IpAddress} " + $"remote-as {neighbour.AsNumber()}");
                builder.AppendLine($" neighbor {neighbour.Ipv4Address.IpAddress} send-community both");
                ipv4AddressFamily.Add($"  neighbor {neighbour.Ipv4Address.IpAddress} activate");

                ipv4AddressFamily.Add($"  neighbor {neighbour.Ipv4Address.IpAddress} " +
                                      $"route-map {BgpPolicyConfig.GetInboundRouteMapName(neighbour.Neighbour!.Bgp,
                                          neighbour.AsNumber(),
                                          neighbour.ParentRouter.Name)} in");

                ipv4AddressFamily.Add($"  neighbor {neighbour.Ipv4Address.IpAddress} " +
                                      $"route-map {BgpPolicyConfig.GetOutboundRouteMapName(neighbour.Neighbour!.Bgp,
                                          neighbour.AsNumber(),
                                          neighbour.ParentRouter.Name)} out");
            }

            // ReSharper disable once InvertIf
            if (neighbour.Ipv6Address is not null)
            {
                builder.AppendLine($" neighbor {neighbour.Ipv6Address.IpAddress} remote-as {neighbour.AsNumber()}");
                builder.AppendLine($" neighbor {neighbour.Ipv6Address.IpAddress} send-community both");
                ipv6AddressFamily.Add($"  neighbor {neighbour.Ipv6Address.IpAddress} activate");

                ipv6AddressFamily.Add($"  neighbor {neighbour.Ipv6Address.IpAddress} " +
                                      $"route-map {BgpPolicyConfig.GetInboundRouteMapName(neighbour.Neighbour!.Bgp,
                                          neighbour.AsNumber(),
                                          neighbour.ParentRouter.Name)} in");

                ipv6AddressFamily.Add($"  neighbor {neighbour.Ipv6Address.IpAddress} " +
                                      $"route-map {BgpPolicyConfig.GetOutboundRouteMapName(neighbour.Neighbour!.Bgp,
                                          neighbour.AsNumber(),
                                          neighbour.ParentRouter.Name)} out");
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
                builder.AppendLine($" neighbor {addressV4} send-community both");
                ipv4AddressFamily.Add($"  neighbor {addressV4} activate");
                ipv4AddressFamily.Add($"  neighbor {addressV4} next-hop-self");
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
        {
            if (network.BaseAddress.AddressFamily == AddressFamily.InterNetwork)
                ipv4AddressFamily.Add($"  network {network.BaseAddress} mask " +
                                      $"{Ipv4AddressUtils.GetV4Mask(network.PrefixLength)} route-map {BgpPolicyConfig.SetLocalRouteMapName}");
            else
            {
                ipv6AddressFamily.Add(
                    $"  network {network.BaseAddress}/{network.PrefixLength} route-map {BgpPolicyConfig.SetLocalRouteMapName}");
            }
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

        builder.AppendLine("!\n!");
    }
}