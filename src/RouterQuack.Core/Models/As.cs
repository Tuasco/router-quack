using System.Diagnostics.Contracts;
using System.Net;
using System.Text;

namespace RouterQuack.Core.Models;

public class As
{
    public required int Number { get; init; }

    public required IgpType Igp { get; init; }

    public required IPNetwork? LoopbackSpace { get; init; }

    public required IPNetwork? NetworksSpace { get; init; }

    public required ICollection<Router> Routers { get; set; }

    [Pure]
    public override string ToString()
    {
        var str = new StringBuilder();
        str.Append($"AS number {Number} ");

        if (Routers.Any(r => r.External))
            str.AppendLine($"(external):");
        else
            str.AppendLine($"using {Igp.ToString().ToUpper()}:");

        foreach (var router in Routers)
            str.AppendLine(router.ToString());

        return str.ToString();
    }
}

public enum IgpType
{
    Ibgp
}