using System.Net;
using System.Net.Sockets;
using System.Text;
using RouterQuack.Core.Models;

namespace RouterQuack.IO.Cisco.Utils;

internal static class InterfacesConfig
{
    internal static void ApplyInterfacesConfig(StringBuilder builder, Router router)
    {
        ApplyLoopbackConfig(builder, router.LoopbackAddressV4, router.LoopbackAddressV6);

        builder.AppendLine(InterfacesConfigHeader);
        foreach (var @interface in router.Interfaces)
            ApplyInterfaceConfig(builder, @interface);
    }

    private const string InterfacesConfigHeader = "! ================= INTERFACES =================";

    private static void ApplyLoopbackConfig(StringBuilder builder, IPAddress? loopbackV4, IPAddress? loopbackV6)
    {
        if (loopbackV4 is null && loopbackV6 is null)
            return;

        builder.AppendLine(LoopbackConfigStart);

        if (loopbackV4 is not null)
        {
            builder.AppendLine($" ip address {loopbackV4} 255.255.255.255");
            builder.AppendLine(" ip ospf 1 area 0");
        }

        if (loopbackV6 is not null)
        {
            builder.AppendLine($" ipv6 address {loopbackV6}/128");
            builder.AppendLine(" ipv6 ospf 1 area 0");
        }

        builder.AppendLine("!\n!");
    }

    private const string LoopbackConfigStart =
        """
        ! ================= LOOPBACK =================
        interface Loopback0
        """;

    private static void ApplyInterfaceConfig(StringBuilder builder, Interface @interface)
    {
        builder.AppendLine($"interface {@interface.Name}");
        builder.AppendLine(InterfaceConfigStart);

        // IPv4
        var ipv4Address =
            @interface.Addresses.FirstOrDefault(a => a.IpAddress.AddressFamily == AddressFamily.InterNetwork)
            ?? @interface.Ipv4Address;

        if (ipv4Address is not null)
        {
            builder.AppendLine(
                $" ip address {ipv4Address.IpAddress} " +
                $"{Ipv4AddressUtils.GetV4Mask(ipv4Address.NetworkAddress.PrefixLength)}");

            if (@interface.Neighbour!.ParentRouter.ParentAs == @interface.ParentRouter.ParentAs)
                builder.AppendLine(" ip ospf 1 area 0");
        }
        else
            builder.AppendLine(" no ip address");

        // IPv6
        var ipv6Addresses = @interface.Addresses
            .Where(a => a.IpAddress.AddressFamily == AddressFamily.InterNetworkV6)
            .ToList();

        if (@interface.Ipv6Address is not null)
            ipv6Addresses.Insert(0, @interface.Ipv6Address);

        if (ipv6Addresses.Any())
            builder.AppendLine(" ipv6 enable");

        foreach (var address in ipv6Addresses)
            builder.AppendLine($" ipv6 address {address.IpAddress}/{address.NetworkAddress.PrefixLength}");

        if (ipv6Addresses.Any() && @interface.Neighbour!.ParentRouter.ParentAs == @interface.ParentRouter.ParentAs)
            builder.AppendLine(" ipv6 ospf 1 area 0");

        // Write MPLS config
        if (@interface.ParentRouter.ParentAs.Igp == IgpType.MPLS)
            builder.AppendLine(" mpls ip");

        // Write additional config is specified
        if (@interface.AdditionalConfig is not null)
        {
            builder.AppendLine("!");
            builder.AppendLine(' ' + @interface.AdditionalConfig.Replace("\n", "\n ").TrimEnd());
        }

        builder.AppendLine("!\n!");
    }

    private const string InterfaceConfigStart =
        """
         no ip proxy-arp
         no ip redirects
         no ipv6 redirects
         negotiation auto
        """;
}