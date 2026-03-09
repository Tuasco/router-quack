using System.Net;
using System.Net.Sockets;
using System.Text;
using RouterQuack.Core.Models;

namespace RouterQuack.IO.Cisco.Utils;

internal static class InterfacesConfig
{
    internal static void ApplyInterfacesConfig(StringBuilder builder, Router router)
    {
        ApplyLoopbackConfig(builder, router.LoopbackAddress!.IpAddress);

        builder.AppendLine(InterfacesConfigHeader);
        foreach (var @interface in router.Interfaces)
            ApplyInterfaceConfig(builder, @interface);
    }

    private const string InterfacesConfigHeader = "! ================= INTERFACES =================";

    private static void ApplyLoopbackConfig(StringBuilder builder, IPAddress loopback)
    {
        builder.AppendLine(LoopbackConfigStart);

        if (loopback.AddressFamily == AddressFamily.InterNetwork)
        {
            builder.AppendLine($" ip address {loopback} 255.255.255.255");
            builder.AppendLine(" ip ospf 1 area 0");
        }
        else
        {
            builder.AppendLine($" ipv6 address {loopback}/128");
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
        var ipv4Addresses = @interface.Addresses
            .Where(a => a.IpAddress.AddressFamily == AddressFamily.InterNetwork).ToArray();

        // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
        if (!ipv4Addresses.Any())
            builder.AppendLine(" no ip address");
        else if (@interface.Bgp == BgpRelationship.None)
            builder.AppendLine(" ip ospf 1 area 0");

        foreach (var address in ipv4Addresses)
            builder.AppendLine($" ip address {address.IpAddress} {GetV4Mask(address.NetworkAddress.PrefixLength)}");

        // IPv6
        var ipv6Addresses = @interface.Addresses
            .Where(a => a.IpAddress.AddressFamily == AddressFamily.InterNetworkV6).ToArray();

        if (ipv6Addresses.Any())
        {
            builder.AppendLine(" ipv6 enable");
            if (@interface.Bgp == BgpRelationship.None)
                builder.AppendLine(" ipv6 ospf 1 area 0");
        }

        foreach (var address in ipv6Addresses)
            builder.AppendLine($" ipv6 address {address.IpAddress}/{address.NetworkAddress.PrefixLength}");

        builder.AppendLine("!\n!");
        return;

        static string GetV4Mask(int subnet)
        {
            var mask = (0xffffffffL << (32 - subnet)) & 0xffffffffL;
            mask = IPAddress.HostToNetworkOrder((int)mask);
            return new IPAddress((uint)mask).ToString();
        }
    }

    private const string InterfaceConfigStart =
        """
         no ip proxy-arp
         no ip redirects
         no ipv6 redirects
         negotiation auto
        """;
}