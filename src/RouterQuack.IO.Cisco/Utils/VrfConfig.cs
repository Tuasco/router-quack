using System.Text;
using RouterQuack.Core.Models;
using Router = RouterQuack.Core.Models.Router;

namespace RouterQuack.IO.Cisco.Utils;

internal static class VrfConfig
{
    internal static void ApplyVrfConfig(StringBuilder builder, Router router)
    {
        if (!router.Vrfs.Any() || !router.BorderRouter)
            return;

        builder.AppendLine(ConfigHeader);

        foreach (var vrf in router.Vrfs)
        {
            builder.AppendLine($"vrf definition {vrf.Name}");
            builder.AppendLine($" rd {vrf.RouteDistinguisher}");

            // address-family ipv4
            if (router.ParentAs.AddressFamily.HasFlag(IpVersion.IPv4)) // Should always be true
            {
                builder.AppendLine(" !");
                builder.AppendLine(" address-family ipv4");
                foreach (var rt in vrf.ImportTargets ?? [])
                    builder.AppendLine($"  route-target import {rt}");
                foreach (var rt in vrf.ExportTargets ?? [])
                    builder.AppendLine($"  route-target export {rt}");
                builder.AppendLine("  exit-address-family");
            }

            // address-family ipv6
            if (router.ParentAs.AddressFamily.HasFlag(IpVersion.IPv6))
            {
                builder.AppendLine(" !");
                builder.AppendLine(" address-family ipv6");
                foreach (var rt in vrf.ImportTargets ?? [])
                    builder.AppendLine($"  route-target import {rt}");
                foreach (var rt in vrf.ExportTargets ?? [])
                    builder.AppendLine($"  route-target export {rt}");
                builder.AppendLine("  exit-address-family");
            }

            builder.AppendLine("!");
        }
    }

    private const string ConfigHeader = "! ================= VRF ================";
}