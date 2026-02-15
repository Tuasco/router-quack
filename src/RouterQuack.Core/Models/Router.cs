using System.Diagnostics.Contracts;
using System.Net;
using System.Text;

namespace RouterQuack.Core.Models;

public sealed class Router
{
    public required string Name { get; init; }

    public required IPAddress Id { get; init; }

    public required RouterBrand Brand { get; init; }

    public required Address? LoopbackAddress { get; set; }

    public required int OspfArea { get; init; }

    public required bool External { get; init; }

    public required ICollection<Interface> Interfaces { get; set; }

    public required As ParentAs { get; init; }

    [Pure]
    public override string ToString()
    {
        var str = new StringBuilder();
        str.AppendLine($"* {Brand.ToString()} router {Name}:");

        foreach (var @interface in Interfaces)
            str.AppendLine(@interface.ToString());

        return str.ToString().TrimEnd('\n');
    }
}

public enum RouterBrand
{
    Cisco
}