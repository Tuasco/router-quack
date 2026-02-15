using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace RouterQuack.IO.Yaml.Models;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public class Router
{
    public IPAddress? Id { get; init; }

    public string? Brand { get; init; }

    public string? Loopback { get; init; }

    public int OspfArea { get; init; } = 0;

    public bool? External { get; init; }

    public required IDictionary<string, Interface> Interfaces { get; init; }
}