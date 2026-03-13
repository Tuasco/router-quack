using System.Net;
using System.Text;

namespace RouterQuack.IO.Cisco.Utils;

internal static class OspfConfig
{
    internal static void ApplyOspfConfig(StringBuilder builder, IPAddress routerId)
    {
        builder.AppendLine(ConfigV6);
        builder.AppendLine($" router-id {routerId}");
        builder.AppendLine("!");

        builder.AppendLine(ConfigV4);
        builder.AppendLine($" router-id {routerId}");
        builder.AppendLine("!\n!");
    }

    private const string ConfigV6 =
        """
        ! ================= OSPFv3 =================
        ipv6 router ospf 1
         auto-cost reference-bandwidth 100000
        """;

    private const string ConfigV4 =
        """
        router ospf 1
         auto-cost reference-bandwidth 100000
        """;
}