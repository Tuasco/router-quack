using System.Net;

namespace RouterQuack.Models.Yaml;

public class Router
{
    public int OspfArea { get; set; } = 0;
    
    public IPAddress? Id { get; set; }
    
    public string? Brand { get; set; }
    
    public required IDictionary<string, Interface> Interfaces { get; set; }
}