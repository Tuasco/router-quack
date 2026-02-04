using System.Net;

namespace RouterQuack.IO.Yaml.Models;

public class Router
{
    public int OspfArea { get; init; } = 0;
    
    public IPAddress? Id { get; init; }
    
    public string? Brand { get; init; }
    
    public bool? External { get; init; }
    
    public required IDictionary<string, Interface> Interfaces { get; init; }
}