using System.Diagnostics.Contracts;
using System.Net;
using System.Text;

namespace RouterQuack.Core.Models;

public sealed class Router
{
    public required string Name { get; init; }

    public IPAddress? Id { get; set; }

    public required RouterBrand Brand { get; init; }

    public IPAddress? LoopbackAddressV4 { get; set; }

    public IPAddress? LoopbackAddressV6 { get; set; }

    public required BgpConfig Bgp { get; init; }

    public required bool External { get; init; }

    public required ICollection<Interface> Interfaces { get; set; }

    public required As ParentAs { get; init; }

    public ICollection<Vrf> Vrfs { get; set; } = [];

    /// <summary>
    /// <c>true</c> if at least one interface has an eBGP neighbour
    /// </summary>
    public bool BorderRouter => Interfaces.Any(i => i.Bgp != BgpRelationship.None);

    [Pure]
    public override string ToString()
    {
        var str = new StringBuilder();
        str.AppendLine($"* {Brand.ToString()} router {Name} (ID: {Id?.ToString() ?? "none"}):");

        foreach (var @interface in Interfaces)
            str.AppendLine(@interface.ToString());

        return str.ToString().TrimEnd('\n');
    }
}

public sealed class BgpConfig
{
    public bool Ibgp { get; set; } = false;

    public IPNetwork[] Networks { get; init; } = [];
}

public enum RouterBrand
{
    Cisco
}