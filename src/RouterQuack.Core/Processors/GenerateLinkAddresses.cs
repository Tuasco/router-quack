using System.Net;
using System.Net.Sockets;
using RouterQuack.Core.Extensions;
using RouterQuack.Core.Utils;

namespace RouterQuack.Core.Processors;

public class GenerateLinkAddresses(
    ILogger<GenerateLinkAddresses> logger,
    Context context,
    NetworkUtils networkUtils,
    InterfaceUtils interfaceUtils) : IProcessor
{
    public bool ErrorsOccurred { get; set; }
    public string BeginMessage => "Generating addresses for interfaces";
    public ILogger Logger { get; } = logger;
    public Context Context { get; } = context;

    private UInt128 _addressCount;
    private HashSet<IPAddress> _usedAddresses = null!;
    private const IpVersion BothVersions = IpVersion.Ipv6 | IpVersion.Ipv4;

    public void Process()
    {
        _usedAddresses = Context.Asses
            .SelectMany(a => a.Routers)
            .SelectMany(r => r.Interfaces)
            .SelectMany(i => i.Addresses)
            .Select(a => a.IpAddress)
            .ToHashSet();

        var links = Context.Asses.GetAllLinks(i => !interfaceUtils.HasLinkNetwork(i));

        foreach (var link in links)
        {
            if (link.Item1.ParentRouter.External || link.Item2.ParentRouter.External)
            {
                this.Log(link.Item1,
                    "Self or neighbour is in an external router, yet no valid link addresses were assigned");
                continue;
            }

            var generateIpv4 =
                (link.Item1.ParentRouter.ParentAs.NetworksIpVersion & IpVersion.Ipv4) == IpVersion.Ipv4
                || (link.Item2.ParentRouter.ParentAs.NetworksIpVersion & IpVersion.Ipv4) == IpVersion.Ipv4;

            var generateIpv6 =
                (link.Item1.ParentRouter.ParentAs.NetworksIpVersion & IpVersion.Ipv6) == IpVersion.Ipv6
                || (link.Item2.ParentRouter.ParentAs.NetworksIpVersion & IpVersion.Ipv6) == IpVersion.Ipv6;

            if (generateIpv4)
                AssignIpAddress(link, IpVersion.Ipv4);

            if (generateIpv6)
                AssignIpAddress(link, IpVersion.Ipv6);
        }
    }

    /// <summary>
    /// Assign an IP address of type <paramref name="ipVersion"/> to the interface and its neighbour.
    /// </summary>
    /// <param name="link">The interface to assign an IP address to.</param>
    /// <param name="ipVersion">The IP version to use.</param>
    private void AssignIpAddress(Tuple<Interface, Interface> link, IpVersion ipVersion)
    {
        var space = ipVersion switch
        {
            IpVersion.Ipv4 =>
                link.Item1.ParentRouter.ParentAs.NetworksSpaceV4
                ?? link.Item2.ParentRouter.ParentAs.NetworksSpaceV4,

            IpVersion.Ipv6 =>
                link.Item1.ParentRouter.ParentAs.NetworksSpaceV6
                ?? link.Item2.ParentRouter.ParentAs.NetworksSpaceV6,

            _ => null
        };

        if (space is null)
        {
            this.Log(link.Item1, $"Cannot generate {ipVersion.ToString()} addresses for self and neighbour " +
                                 "because the expected networks space is not defined");
            return;
        }

        // About to generate a second IPv4 address
        var match = false;

        if (space.Value.BaseAddress.AddressFamily == AddressFamily.InterNetwork
            && link.Item1.Addresses.Any(a => a.IpAddress.AddressFamily == AddressFamily.InterNetwork))
        {
            // Generate warning only if an IPv6 address has been or will be generated
            var logLevel = link.Item1.ParentRouter is { External: false, ParentAs.NetworksIpVersion: BothVersions }
                ? LogLevel.Warning
                : LogLevel.Error;
            this.Log(link.Item1, "Already has an IPv4 address", logLevel: logLevel);
            match = true;
        }

        if (space.Value.BaseAddress.AddressFamily == AddressFamily.InterNetwork
            && link.Item2.Addresses.Any(a => a.IpAddress.AddressFamily == AddressFamily.InterNetwork))
        {
            var logLevel = link.Item2.ParentRouter is { External: false, ParentAs.NetworksIpVersion: BothVersions }
                ? LogLevel.Warning
                : LogLevel.Error;
            this.Log(link.Item2, "Already has an IPv4 address", logLevel: logLevel);
            match = true;
        }

        if (match)
            return;

        var maxBits = space.Value.BaseAddress.AddressFamily == AddressFamily.InterNetworkV6 ? 128 : 32;
        IPAddress ip1, ip2;
        try
        {
            ip1 = networkUtils.GenerateAvailableIpAddress(space.Value, ref _addressCount, _usedAddresses);
            ip2 = networkUtils.GenerateAvailableIpAddress(space.Value, ref _addressCount, _usedAddresses);
        }
        catch (InvalidOperationException)
        {
            this.Log(link.Item1.ParentRouter.ParentAs, $"{ipVersion.ToString()} networks space has overflowed");
            return;
        }

        var linkNetwork = new IPNetwork(ip1, maxBits - 1);
        link.Item1.Addresses.Add(new(linkNetwork, ip1));
        link.Item2.Addresses.Add(new(linkNetwork, ip2));

        logger.LogDebug("Generated link {IpNetwork} between {Router1}:{Interface1} and {Router2}:{Interface2}",
            linkNetwork,
            link.Item1.ParentRouter.Name, link.Item1.Name,
            link.Item2.ParentRouter.Name, link.Item2.Name);
    }
}