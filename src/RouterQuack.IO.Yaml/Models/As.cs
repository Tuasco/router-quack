using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace RouterQuack.IO.Yaml.Models;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public class As
{
    public string? Igp { get; init; }

    public IPNetwork? LoopbackSpace { get; init; }

    public IPNetwork? NetworksSpace { get; init; }

    public string? Brand { get; init; }

    public bool External { get; init; } = false;

    public required IDictionary<string, Router> Routers { get; init; }
}