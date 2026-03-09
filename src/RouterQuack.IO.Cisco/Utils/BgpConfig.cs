using System.Text;
using RouterQuack.Core.Models;

namespace RouterQuack.IO.Cisco.Utils;

internal static class BgpConfig
{
    internal static void ApplyBgpConfig(StringBuilder builder, Router router)
    {
        if (!router.BorderRouter)
            return;

        builder.AppendLine(ConfigHeader);
        builder.AppendLine($"router bgp {router.ParentAs.Number}");
        builder.AppendLine($" bgp router id {router.Id}");
        builder.AppendLine(ConfigStart);

        foreach (var neighbour in router.Interfaces.Where(i => i.Bgp != BgpRelationship.None))
            builder.AppendLine(
                $" neighbor {neighbour.Addresses.First()} remote-as {neighbour.ParentRouter.ParentAs.Number}");

        builder.Append(ConfigEnd);
    }

    private const string ConfigHeader = "! ================= BGP =================";

    private const string ConfigStart =
        """
         bgp log-neighbor-changes
         no bgp default ipv4-unicast
        """;

    private const string ConfigEnd =
        """
         !
         address-family ip
          exit
         !
         address-family ipv6
          exit
        !
        !
        """;
}