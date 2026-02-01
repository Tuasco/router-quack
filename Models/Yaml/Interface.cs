using System.Net;

namespace RouterQuack.Models.Yaml;

public class Interface
{
    public required string Neighbour { get; init; }

    public string? Bgp { get; init; }
    
    public ICollection<IPNetwork>? Addresses { get; init; }
}