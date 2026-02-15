using System.Diagnostics.Contracts;
using System.Net;
using System.Text;

namespace RouterQuack.Core.Models;

public sealed class As
{
    public required int Number { get; init; }

    public required IgpType Igp { get; init; }

    public required IPNetwork? LoopbackSpace { get; init; }

    public required IPNetwork? NetworksSpaceV4 { get; init; }

    public required IPNetwork? NetworksSpaceV6 { get; init; }

    public required IpVersion NetworksIpVersion { get; init; }

    public required ICollection<Router> Routers { get; set; }

    /// <summary>
    /// Return true if all the routers in the AS are marked as external.
    /// </summary>
    public bool FullyExternal => Routers.All(r => r.External);

    [Pure]
    public override string ToString()
    {
        var str = new StringBuilder();
        str.Append($"AS number {Number} ");

        if (Routers.Any(r => r.External))
            str.AppendLine($"(external):");
        else
            str.AppendLine($"using {Igp.ToString().ToUpper()}:");

        foreach (var router in Routers)
            str.AppendLine(router.ToString());

        return str.ToString();
    }
}

public enum IgpType
{
    Ibgp
}

[Flags]
public enum IpVersion
{
    Ipv4 = 1,
    Ipv6 = 2
}