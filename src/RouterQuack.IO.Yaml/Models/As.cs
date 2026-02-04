using System.Net;

namespace RouterQuack.IO.Yaml.Models;

public class As
{
    public string? Igp { get; init; }

    public required IPNetwork LoopbackSpace { get; init; }

    public required IPNetwork NetworksSpace { get; init; }
    
    public string? Brand { get; init; }

    public required IDictionary<string, Router> Routers { get; init; }
}