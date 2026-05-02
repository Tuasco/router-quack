using System.Diagnostics.Contracts;
using System.Net;
using System.Text;

namespace RouterQuack.Core.Models;

public sealed class As
{
    public required int Number { get; init; }

    public required IgpType Igp { get; init; }

    public required CoreType Core { get; init; }

    public IPNetwork? LoopbackSpaceV4 { get; init; }

    public IPNetwork? LoopbackSpaceV6 { get; init; }

    public IPNetwork? NetworksSpaceV4 { get; init; }

    public IPNetwork? NetworksSpaceV6 { get; init; }

    public required IpVersion AddressFamily { get; init; }

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
            str.AppendLine($"using {Igp.ToString()} ({AddressFamily}):");

        foreach (var router in Routers)
            str.AppendLine(router.ToString());

        return str.ToString();
    }
}

// ReSharper disable all InconsistentNaming
[Flags]
public enum IgpType
{
    None = 0,
    OSPF = 1
}

[Flags]
public enum CoreType
{
    None = 0,
    iBGP = 1,
    LDP = 2
}

[Flags]
public enum IpVersion
{
    #pragma warning disable CA1069
    None = 0,
    IPv4 = 1,
    v4 = 1,
    IPv6 = 2,
    v6 = 2,
    Both = 3,
    Dual = 3
    #pragma warning restore CA1069
}