using System.Diagnostics.CodeAnalysis;

namespace RouterQuack.IO.Yaml.Models;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class Interface
{
    public required string Neighbour { get; init; }

    public BgpRelationship Bgp { get; init; } = BgpRelationship.None;

    public ICollection<string>? Addresses { get; init; }

    public string? Vrf { get; init; }
}