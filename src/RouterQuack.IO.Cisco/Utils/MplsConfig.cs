using System.Text;
using RouterQuack.Core.Models;

namespace RouterQuack.IO.Cisco.Utils;

internal static class MplsConfig
{
    internal static void ApplyMplsConfigs(StringBuilder builder, Router router)
    {
        if (router.ParentAs.Igp != IgpType.MPLS)
            return;

        builder.AppendLine(MplsConfigHeader);
    }

    private const string MplsConfigHeader =
        """
        ! ================= MPLS =================
        mpls label protocol ldp
        mpls ldp router-id loopback 0 force
        !
        !
        """;
}