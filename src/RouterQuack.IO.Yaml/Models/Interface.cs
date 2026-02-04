namespace RouterQuack.IO.Yaml.Models;

public class Interface
{
    public required string Neighbour { get; init; }

    public string? Bgp { get; init; }
    
    public ICollection<string>? Addresses { get; init; }
}