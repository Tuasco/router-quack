using System.Net;
using System.Net.Sockets;
using System.Text;
using RouterQuack.Core.Models;

namespace RouterQuack.IO.Cisco.Utils;

internal static class InterfacesConfig
{
    internal static void ApplyInterfacesConfig(StringBuilder builder, Router router)
    {
        if (!router.External)
            ApplyLoopbackConfig(builder, router.LoopbackAddress!.IpAddress);
    }

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
}