using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Net;
using System.Text;

namespace RouterQuack.Core.Models;

public sealed class As
{
    public required int Number { get; init; }

    public required IgpType Igp { get; init; }

    public IPNetwork? LoopbackSpaceV4 { get; init; }

    public IPNetwork? LoopbackSpaceV6 { get; init; }

    public IPNetwork? NetworksSpaceV4 { get; init; }

    public IPNetwork? NetworksSpaceV6 { get; init; }

    public required IpVersion IpVersions { get; init; }

    public DeployInfo? Deploy { get; init; }

    public required ICollection<Router> Routers { get; set; }

    /// <summary>
    /// Return true if all the routers in the AS are marked as external.
    /// </summary>
    public bool FullyExternal => Routers.All(r => r.External);

    [Pure]
    public override string ToString()
    {
        var str = new StringBuilder();
        str.Append($"AS {Number} ");

        if (Routers.Any(r => r.External))
            str.AppendLine($"(external):");
        else
            str.AppendLine($"using {Igp.ToString()} ({IpVersions}):");

        foreach (var router in Routers)
            str.AppendLine(router.ToString());

        return str.ToString();
    }
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum IgpType
{
    iBGP,
    OSPF,
    MPLS
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
[Flags]
public enum IpVersion
{
    IPv4 = 1,
    IPv6 = 2
}