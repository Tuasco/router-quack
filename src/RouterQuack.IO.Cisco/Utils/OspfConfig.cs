using System.Net;
using System.Text;
using RouterQuack.Core.Models;

namespace RouterQuack.IO.Cisco.Utils;

internal static class OspfConfig
{
    internal static void ApplyOspfConfig(StringBuilder builder, IPAddress routerId, IpVersion version)
    {
        // To make sure header is inserted without being duplicated
        var headerSet = false;

        if (version.HasFlag(IpVersion.IPv6))
        {
            builder.AppendLine(ConfigHeader);
            headerSet = true;

            builder.AppendLine(ConfigV6);
            builder.AppendLine($" router-id {routerId}");
            builder.AppendLine("!");
        }

        // ReSharper disable once InvertIf
        if (version.HasFlag(IpVersion.IPv4))
        {
            if (!headerSet)
                builder.AppendLine(ConfigHeader);

            builder.AppendLine(ConfigV4);
            builder.AppendLine($" router-id {routerId}");
            builder.AppendLine("!\n!");
        }
    }

    private const string ConfigHeader = "! ================= OSPF =================";

    private const string ConfigV6 =
        """
        ipv6 router ospf 1
         auto-cost reference-bandwidth 100000
        """;

    private const string ConfigV4 =
        """
        router ospf 1
         auto-cost reference-bandwidth 100000
        """;
}