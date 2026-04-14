using System.Text;
using RouterQuack.Core.Models;

namespace RouterQuack.IO.Cisco.Utils;

internal static class OspfConfig
{
    internal static void ApplyOspfConfig(StringBuilder builder, Router router)
    {
        // To make sure header is inserted without being duplicated
        var headerSet = false;

        // Make sure not to enable OSPF for IPv6 when LDP the core protocol.
        if (router.ParentAs.AddressFamily.HasFlag(IpVersion.IPv6) && router.ParentAs.Core != CoreType.LDP)
        {
            builder.AppendLine(ConfigHeader);
            headerSet = true;

            builder.AppendLine(ConfigV6);
            builder.AppendLine($" router-id {router.Id!}");
            builder.AppendLine("!");
        }

        // ReSharper disable once InvertIf
        if (router.ParentAs.AddressFamily.HasFlag(IpVersion.IPv4))
        {
            if (!headerSet)
                builder.AppendLine(ConfigHeader);

            builder.AppendLine(ConfigV4);
            builder.AppendLine($" router-id {router.Id!}");
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