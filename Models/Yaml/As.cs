using System.Net;

namespace RouterQuack.Models.Yaml;

public class As
{
    public required string Igp { get; set; }

    public required IPNetwork LoopbackSpace { get; set; }

    public required IPNetwork NetworksSpace { get; set; }

    public required IDictionary<string, Router> Routers { get; set; }
}