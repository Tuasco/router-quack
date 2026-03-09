using System.Text;

namespace RouterQuack.IO.Cisco.Utils;

internal static class UnusedServicesConfig
{
    internal static void ApplyUnusedServicesConfig(StringBuilder builder)
    {
        builder.AppendLine(Config);
    }

    private const string Config =
        """
        ! ================= Unused services =================
        no ip forward-protocol nd
        no ip http secure-server
        no ip http server
        no ip bootp server
        no ip source-route
        no service tcp-small-servers
        no service udp-small-servers
        no service finger
        no service config
        no snmp-server
        no cdp run
        no lldp run
        !
        !
        """;
}