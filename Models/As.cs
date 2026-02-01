using System.Net;

namespace RouterQuack.Models;

public class As
{
    public required int Number { get; init; }
    
    public required IgpType Igp { get; init; }

    public required IPNetwork LoopbackSpace { get; init; }

    public required IPNetwork NetworksSpace { get; init; }

    public required ICollection<Router> Routers { get; set; }
}

public enum IgpType
{
    Ibgp
}