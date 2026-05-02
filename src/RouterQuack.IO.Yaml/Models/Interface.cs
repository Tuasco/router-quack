using System.Diagnostics.CodeAnalysis;

namespace RouterQuack.IO.Yaml.Models;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public sealed class Interface
{
    public required object Neighbour { get; init; }

    public BgpRelationship Bgp { get; init; } = BgpRelationship.None;

    public ICollection<string>? Addresses { get; init; }

    public string? Vrf { get; init; }

    public string? AdditionalConfig { get; init; }
}