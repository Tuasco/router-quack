using System.Diagnostics.CodeAnalysis;

namespace RouterQuack.IO.Yaml.Models;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public sealed class Interface
{
    public required object Neighbour { get; init; }

    public BgpRelationship Bgp { get; init; } = BgpRelationship.None;

    public ICollection<string>? Addresses { get; init; }
}

// ReSharper disable once ClassNeverInstantiated.Global
// ReSharper disable twice MemberCanBePrivate.Global
public sealed class NeighbourInfo
{
    public int As { get; set; }

    public required string Router { get; init; }

    public string? Interface { get; init; }

    // This constructor won't correctly populate Neighbour Info if it is of this type:
    // RouterName:InterfaceName, when RouterName is an int.
    [SetsRequiredMembers]
    public NeighbourInfo(string neighbour, int defaultAs)
    {
        var segments = neighbour.Split(':');

        (As, Router, Interface) = segments.Length switch
        {
            1 => new(defaultAs, segments[0], null),
            2 when int.TryParse(segments[0], out var asNumber) => (asNumber, segments[1], null),
            2 => (defaultAs, segments[0], segments[1]),
            3 when int.TryParse(segments[0], out var asNumber) => (asNumber, segments[1], segments[2]),
            _ => (defaultAs, string.Empty, null)
        };
    }

    [SetsRequiredMembers]
    public NeighbourInfo(int asNumber, string routerName, string? interfaceName)
        => (As, Router, Interface) = (asNumber, routerName, interfaceName);

    public override string ToString()
        => $"{As}:{Router}:{Interface}";
}