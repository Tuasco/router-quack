using System.Text;
using RouterQuack.Core.Models;

namespace RouterQuack.IO.Cisco.Utils;

internal static class AdditionalRouterConfig
{
    internal static void ApplyAdditionalRouterConfig(StringBuilder builder, Router router)
    {
        if (router.AdditionalConfig is null)
            return;

        builder.AppendLine("! ================= AdditionalConfig =================");
        builder.AppendLine(router.AdditionalConfig.TrimEnd());
        builder.AppendLine("!\n!");
    }
}