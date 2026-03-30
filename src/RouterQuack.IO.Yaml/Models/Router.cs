using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace RouterQuack.IO.Yaml.Models;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public sealed class Router
{
    public IPAddress? Id { get; init; }

    public RouterBrand? Brand { get; init; }

    public IPAddress? LoopbackV4 { get; init; }

    public IPAddress? LoopbackV6 { get; init; }

    public BgpConfig Bgp { get; init; } = new();

    public bool? External { get; init; }

    public string? AdditionalConfig { get; init; }

    public required IDictionary<string, YamlInterface> Interfaces { get; init; }
}