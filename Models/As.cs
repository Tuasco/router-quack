using System.Net;

namespace RouterQuack.Models;

public class As
{
    public required int Number { get; set; }
    
    public required IgpType Igp { get; set; }

    public required IPNetwork LoopbackSpace { get; set; }

    public required IPNetwork NetworksSpace { get; set; }

    public required ICollection<Router> Routers { get; set; }
}

public enum IgpType
{
    Ibgp
}