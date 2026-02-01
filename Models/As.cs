using System.Net;
using System.Text;

namespace RouterQuack.Models;

public class As
{
    public required int Number { get; init; }
    
    public required IgpType Igp { get; init; }

    public required IPNetwork LoopbackSpace { get; init; }

    public required IPNetwork NetworksSpace { get; init; }

    public required ICollection<Router> Routers { get; set; }

    public override string ToString()
    {
        var str = new StringBuilder();
        str.AppendLine($"AS number {Number} using {Igp.ToString().ToUpper()}:");
        
        foreach (var router in Routers)
            str.AppendLine(router.ToString());
        
        return str.ToString();
    }
}

public enum IgpType
{
    Ibgp
}