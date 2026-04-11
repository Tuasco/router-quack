using System.Net;

namespace RouterQuack.IO.Yaml.Models;

// ReSharper disable once ClassNeverInstantiated
// ReSharper disable all UnusedAutoPropertyAccessor
public sealed class As
{
    public IgpType Igp { get; init; } = IgpType.OSPF;

    public CoreType Core { get; init; } = CoreType.iBGP;

    public IPNetwork? LoopbackSpaceV4 { get; init; }

    public IPNetwork? LoopbackSpaceV6 { get; init; }

    public IPNetwork? NetworksSpaceV4 { get; init; }

    public IPNetwork? NetworksSpaceV6 { get; init; }

    public IpVersion AddressFamily { get; init; } = IpVersion.Both;

    public RouterBrand Brand { get; init; } = RouterBrand.Cisco;

    public DeployInfo? Deploy { get; init; }

    public bool External { get; init; } = false;

    public required IDictionary<string, YamlRouter> Routers { get; init; }
}