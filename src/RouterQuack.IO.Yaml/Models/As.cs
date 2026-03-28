using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace RouterQuack.IO.Yaml.Models;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public sealed class As
{
    public IgpType Igp { get; init; } = IgpType.iBGP;

    public IPNetwork? LoopbackSpaceV4 { get; init; }

    public IPNetwork? LoopbackSpaceV6 { get; init; }

    public IPNetwork? NetworksSpaceV4 { get; init; }

    public IPNetwork? NetworksSpaceV6 { get; init; }

    public IpVersion Networks { get; init; } = IpVersion.IPv4 | IpVersion.IPv6;

    public RouterBrand Brand { get; init; } = RouterBrand.Cisco;

    public bool External { get; init; } = false;

    public required IDictionary<string, YamlRouter> Routers { get; init; }
}