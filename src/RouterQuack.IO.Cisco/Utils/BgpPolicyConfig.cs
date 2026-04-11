using System.Text;
using RouterQuack.Core.Models;

namespace RouterQuack.IO.Cisco.Utils;

internal static class BgpPolicyConfig
{
    internal const string SetLocalRouteMapName = "RM-SET-LOCAL";
    private const string PolicyHeader = "! ================= BGP Policy =================";
    private const string InternalScrubListName = "CL-INTERNAL-SCRUB";
    private const string InternalStripListName = "CL-INTERNAL-STRIP";

    internal static void ApplyPolicyConfig(StringBuilder builder, int asNumber, IEnumerable<Interface> ebgpInterfaces)
    {
        builder.AppendLine(PolicyHeader);
        AppendCommunityLists(builder, asNumber);
        AppendSetLocalRouteMap(builder, asNumber);

        foreach (var @interface in ebgpInterfaces)
        {
            if (@interface.Bgp == BgpRelationship.None)
                continue;

            AppendRouteMaps(builder, asNumber, @interface.Neighbour!.ParentRouter, @interface.Bgp);
        }

        builder.AppendLine("!");
    }

    internal static string GetInboundRouteMapName(BgpRelationship relationship, int asNumber, string neighbourName)
        => $"RM-IN-{relationship.ToString().ToUpperInvariant()}-{asNumber}-{neighbourName}";

    internal static string GetOutboundRouteMapName(BgpRelationship relationship, int asNumber, string neighbourName)
        => $"RM-OUT-{relationship.ToString().ToUpperInvariant()}-{asNumber}-{neighbourName}";

    private static int GetLocalPreference(BgpRelationship relationship)
        => relationship switch
        {
            BgpRelationship.Client => 300,
            BgpRelationship.Peer => 200,
            BgpRelationship.Provider => 100,
            _ => throw new ArgumentOutOfRangeException(nameof(relationship), relationship, null)
        };

    private static int GetCommunitySuffix(BgpRelationship? relationship)
        => relationship switch
        {
            null => 2000,
            BgpRelationship.Client => 2100,
            BgpRelationship.Peer => 2200,
            BgpRelationship.Provider => 2300,
            _ => throw new ArgumentOutOfRangeException(nameof(relationship), relationship, null)
        };

    private static string GetSourceCommunityListName(BgpRelationship? relationship)
        => relationship switch
        {
            null => "RQ-SRC-LOCAL",
            _ => $"RQ-SRC-{relationship.ToString()!.ToUpperInvariant()}"
        };

    private static string GetCommunityValue(int asNumber, BgpRelationship? relationship)
        => $"{asNumber}:{GetCommunitySuffix(relationship)}";

    private static void AppendCommunityLists(StringBuilder builder, int asNumber)
    {
        foreach (var relationship in GetSourceRelationships())
            builder.AppendLine($"ip community-list standard {GetSourceCommunityListName(relationship)} " +
                               $"permit {GetCommunityValue(asNumber, relationship)}");

        builder.AppendLine($"ip community-list standard {InternalScrubListName} " +
                           $"permit {GetCommunityValue(asNumber, null)}");
        foreach (var relationship in GetNeighbourRelationships())
            builder.AppendLine($"ip community-list standard {InternalScrubListName} " +
                               $"permit {GetCommunityValue(asNumber, relationship)}");

        builder.AppendLine($"ip community-list standard {InternalStripListName} " +
                           $"permit {GetCommunityValue(asNumber, null)}");
        foreach (var relationship in GetNeighbourRelationships())
            builder.AppendLine($"ip community-list standard {InternalStripListName} " +
                               $"permit {GetCommunityValue(asNumber, relationship)}");

        builder.AppendLine("!");
    }

    private static void AppendSetLocalRouteMap(StringBuilder builder, int asNumber)
    {
        builder.AppendLine($"route-map {SetLocalRouteMapName} permit 10");
        builder.AppendLine($" set community {GetCommunityValue(asNumber, null)} additive");
        builder.AppendLine("!");
    }

    private static void AppendRouteMaps(StringBuilder builder,
        int asNumber,
        Router neighbour,
        BgpRelationship relationship)
    {
        var inboundName = GetInboundRouteMapName(relationship, neighbour.ParentAs.Number, neighbour.Name);
        builder.AppendLine($"route-map {inboundName} permit 10");
        builder.AppendLine($" set comm-list {InternalScrubListName} delete");
        builder.AppendLine($" set local-preference {GetLocalPreference(relationship)}");
        builder.AppendLine($" set community {GetCommunityValue(asNumber, relationship)} additive");
        builder.AppendLine("!");

        var outboundName = GetOutboundRouteMapName(relationship, neighbour.ParentAs.Number, neighbour.Name);
        var sequence = 10;

        foreach (var allowedRelationship in GetAllowedExportRelationships(relationship))
        {
            builder.AppendLine($"route-map {outboundName} permit {sequence}");
            builder.AppendLine($" match community {GetSourceCommunityListName(allowedRelationship)}");
            builder.AppendLine($" set comm-list {InternalStripListName} delete");

            const int routeMapSequenceStep = 10;
            sequence += routeMapSequenceStep;
        }

        builder.AppendLine($"route-map {outboundName} deny {sequence}");
        builder.AppendLine("!");
    }

    private static BgpRelationship?[] GetSourceRelationships()
        => [null, .. GetNeighbourRelationships()];

    private static BgpRelationship[] GetNeighbourRelationships()
        => [BgpRelationship.Client, BgpRelationship.Peer, BgpRelationship.Provider];

    private static BgpRelationship?[] GetAllowedExportRelationships(BgpRelationship relationship)
        => relationship switch
        {
            BgpRelationship.Client => [null, BgpRelationship.Client, BgpRelationship.Peer, BgpRelationship.Provider],
            BgpRelationship.Peer or BgpRelationship.Provider => [null, BgpRelationship.Client],
            _ => throw new ArgumentOutOfRangeException(nameof(relationship), relationship, null)
        };
}