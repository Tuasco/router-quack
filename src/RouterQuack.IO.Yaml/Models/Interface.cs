using System.Diagnostics.CodeAnalysis;

namespace RouterQuack.IO.Yaml.Models;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public class Interface
{
    public required string Neighbour { get; init; }

    public string? Bgp { get; init; }

    [SuppressMessage("ReSharper", "CollectionNeverUpdated.Global")]
    public ICollection<string>? Addresses { get; init; }
}