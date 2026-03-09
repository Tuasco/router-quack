using System.Text;

namespace RouterQuack.IO.Cisco.Utils;

internal static class InitialConfig
{
    internal static void ApplyInitialConfig(StringBuilder builder, string hostname)
    {
        builder.AppendLine(PreBootConfig);
        builder.AppendLine($"hostname {hostname}\n!");
        builder.AppendLine(BootConfig);
        builder.AppendLine(PostBootConfig);
        builder.AppendLine(ConnectionConfig);
    }

    private const string PreBootConfig =
        """
        !

        ! ================= Global config =================
        version 15.2
        """;

    private const string BootConfig =
        """
        boot-start-marker
        boot-end-marker
        !
        """;

    private const string PostBootConfig =
        """
        no ip domain lookup
        transport preferred none
        !
        !
        """;

    private const string ConnectionConfig =
        """
        no aaa new-model
        line con 0
         exec-timeout 15 0
         no login
         privilege level 15
         stopbits 1
        line aux 0
         no login
         transport input none
        line vty 0 4
         exec-timeout 15 0
         no login
         transport input all
        !
        !
        """;

    private const string IpConfig =
        """
        ! ================= IP config =================
        ip routing
        ip cef
        ipv6 unicast-routing
        ipv6 cef
        ip tcp synwait-time 5
        ip icmp error-interval 0
        !
        !
        """;
}