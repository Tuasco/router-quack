using System.Net;

namespace RouterQuack.Models.Yaml;

public class Router
{
    public int OspfArea { get; init; } = 0;
    
    public IPAddress? Id { get; init; }
    
    public string? Brand { get; init; }
    
    public required IDictionary<string, Interface> Interfaces { get; init; }
}