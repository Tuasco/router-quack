namespace RouterQuack.Models.Yaml;

public class Interface
{
    public required string Neighbour { get; init; }

    public string? Bgp { get; init; }
    
    public ICollection<string>? Addresses { get; init; }
}