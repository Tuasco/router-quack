using System.Net;
using System.Net.Sockets;
using RouterQuack.Core.Extensions;
using RouterQuack.Core.Utils;

namespace RouterQuack.Core.Processors;

/// <summary>
/// Generate link addresses from networks space for selected IP versions (skip if already configured).
/// </summary>
public class GenerateLinkAddresses(
    ILogger<GenerateLinkAddresses> logger,
    Context context,
    NetworkUtils networkUtils) : IProcessor
{
    public string BeginMessage => "Generating addresses for interfaces";
    public ILogger Logger { get; } = logger;
    public Context Context { get; } = context;

    private UInt128 _addressCountV4, _addressCountV6;
    private HashSet<IPAddress> _usedAddresses = null!;
    private const IpVersion BothVersions = IpVersion.IPv6 | IpVersion.IPv4;

    public void Process()
    {
        _usedAddresses = Context.Asses
            .SelectMany(a => a.Routers)
            .SelectMany(r => r.Interfaces)
            .SelectMany(i => i.Addresses)
            .Select(a => a.IpAddress)
            .ToHashSet();

        foreach (var link in Context.Asses.GetAllLinks())
        {
            var addresses = GetLinkNetworks(link).ToArray();

            // If we need to generate IPv4 link addresses
            if (link.Item1.ParentRouter.ParentAs.AddressFamily.HasFlag(IpVersion.IPv4)
                || link.Item2.ParentRouter.ParentAs.AddressFamily.HasFlag(IpVersion.IPv4))
            {
                var ipv4Addresses = addresses.Where(a
                    => a.address.NetworkAddress.BaseAddress.AddressFamily == AddressFamily.InterNetwork).ToArray();

                switch (ipv4Addresses.Length)
                {
                    case 0:
                        AssignIpAddress(link, IpVersion.IPv4);
                        break;

                    case 1:
                        var linkAddresses = ipv4Addresses.First();
                        (link.Item1.Ipv4Address, link.Item2.Ipv4Address) = linkAddresses;
                        link.Item1.Addresses.Remove(linkAddresses.address);
                        link.Item2.Addresses.Remove(linkAddresses.neighbourAddress);
                        break;

                    default:
                        this.Log(link.Item1, "Many IPv4 links between self an neighbour.");
                        break;
                }
            }

            // If we need to generate IPv6 link addresses
            if (link.Item1.ParentRouter.ParentAs.AddressFamily.HasFlag(IpVersion.IPv6)
                || link.Item2.ParentRouter.ParentAs.AddressFamily.HasFlag(IpVersion.IPv6))
            {
                // Skip if this is an MPLS core link
                if (link.Item1.ParentRouter.ParentAs == link.Item2.ParentRouter.ParentAs
                    && link.Item1.ParentRouter.ParentAs.Core == CoreType.LDP)
                    continue;

                var ipv6Addresses = addresses.Where(a
                    => a.address.NetworkAddress.BaseAddress.AddressFamily == AddressFamily.InterNetworkV6).ToArray();

                switch (ipv6Addresses.Length)
                {
                    case 0:
                        AssignIpAddress(link, IpVersion.IPv6);
                        break;

                    case 1:
                        var linkAddresses = ipv6Addresses.First();
                        (link.Item1.Ipv6Address, link.Item2.Ipv6Address) = linkAddresses;
                        link.Item1.Addresses.Remove(linkAddresses.address);
                        link.Item2.Addresses.Remove(linkAddresses.neighbourAddress);
                        break;

                    default:
                        this.Log(link.Item1, "Many IPv6 links between self an neighbour.");
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Assign an IP address of type <paramref name="ipVersion"/> to the interface and its neighbour.
    /// </summary>
    /// <param name="link">The interface to assign an IP address to.</param>
    /// <param name="ipVersion">The IP version to use.</param>
    private void AssignIpAddress((Interface, Interface) link, IpVersion ipVersion)
    {
        var space = ipVersion switch
        {
            IpVersion.IPv4 =>
                link.Item1.ParentRouter.ParentAs.NetworksSpaceV4 ?? link.Item2.ParentRouter.ParentAs.NetworksSpaceV4,

            IpVersion.IPv6 =>
                link.Item1.ParentRouter.ParentAs.NetworksSpaceV6 ?? link.Item2.ParentRouter.ParentAs.NetworksSpaceV6,

            _ => null
        };

        // Check that external routers have manually setup link addresses
        if (link.Item1.ParentRouter.External)
        {
            this.Log(link.Item1, $"External router, " +
                                 $"yet no valid {ipVersion.ToString()} link addresses were assigned");
            return;
        }

        if (link.Item2.ParentRouter.External)
        {
            this.Log(link.Item2, $"External router, " +
                                 $"yet no valid {ipVersion.ToString()} link addresses were assigned");
            return;
        }

        // Check that routers are not in the same AS
        if (link.Item1.ParentRouter.ParentAs.Number != link.Item2.ParentRouter.ParentAs.Number)
            this.Log(link.Item1, $"Routers are in different ASes, " +
                                 $"yet no valid {ipVersion.ToString()} link addresses were assigned", LogLevel.Warning);

        // Check space is valid
        if (space is null)
        {
            this.Log(link.Item1, $"Cannot generate {ipVersion.ToString()} addresses for self and neighbour " +
                                 "because the expected networks space is not defined");
            return;
        }

        // About to generate a second IPv4 address
        if (AlreadyHasIpv4Address(link, space.Value))
            return;

        // Generate and populate IP addresses
        IPAddress ip1, ip2;
        try
        {
            ref var addressCount = ref ipVersion == IpVersion.IPv4 ? ref _addressCountV4 : ref _addressCountV6;
            ip1 = networkUtils.GenerateAvailableIpAddress(space.Value, ref addressCount, _usedAddresses, true);
            ip2 = networkUtils.GenerateAvailableIpAddress(space.Value, ref addressCount, _usedAddresses);
        }
        catch (InvalidOperationException)
        {
            this.Log(link.Item1.ParentRouter.ParentAs, $"{ipVersion.ToString()} networks space has overflowed");
            return;
        }

        var maxBits = ipVersion == IpVersion.IPv6 ? 128 : 32;
        var linkNetwork = new IPNetwork(ip1, maxBits - 1);
        if (ipVersion == IpVersion.IPv4)
        {
            link.Item1.Ipv4Address = new(linkNetwork, ip1);
            link.Item2.Ipv4Address = new(linkNetwork, ip2);
        }
        else
        {
            link.Item1.Ipv6Address = new(linkNetwork, ip1);
            link.Item2.Ipv6Address = new(linkNetwork, ip2);
        }

        logger.LogDebug("Generated link {IpNetwork} between {Router1}:{Interface1} and {Router2}:{Interface2}",
            linkNetwork,
            link.Item1.ParentRouter.Name, link.Item1.Name,
            link.Item2.ParentRouter.Name, link.Item2.Name);
    }

    /// <summary>
    /// Log warning or error if an interface of a link already has an IPv4 address, while trying to generate one.
    /// </summary>
    /// <param name="link">Link to process.</param>
    /// <param name="space">Networks space of the link.</param>
    /// <returns><c>true</c> if a warning or error was logged.</returns>
    private bool AlreadyHasIpv4Address((Interface, Interface) link, IPNetwork space)
    {
        switch (space.BaseAddress.AddressFamily)
        {
            case AddressFamily.InterNetwork
                when link.Item1.Addresses.Any(a => a.IpAddress.AddressFamily == AddressFamily.InterNetwork):
            {
                // Generate warning only if an IPv6 address has been or will be generated
                var logLevel = link.Item1.ParentRouter is { External: false, ParentAs.AddressFamily: BothVersions }
                    ? LogLevel.Warning
                    : LogLevel.Error;
                this.Log(link.Item1, "Already has an IPv4 address", logLevel: logLevel);
                return true;
            }

            case AddressFamily.InterNetwork
                when link.Item2.Addresses.Any(a => a.IpAddress.AddressFamily == AddressFamily.InterNetwork):
            {
                var logLevel = link.Item2.ParentRouter is { External: false, ParentAs.AddressFamily: BothVersions }
                    ? LogLevel.Warning
                    : LogLevel.Error;
                this.Log(link.Item2, "Already has an IPv4 address", logLevel: logLevel);
                return true;
            }

            default:
                return false;
        }
    }

    /// <summary>
    /// Return the <see cref="Address"/>es of a link, if addresses share a common networks.
    /// </summary>
    /// <param name="link">The link to process.</param>
    /// <returns>
    /// A list of valid link addresses (i.e. when the interface's and neighbour's addresses share a common network).
    /// </returns>
    private static IEnumerable<(Address address, Address neighbourAddress)> GetLinkNetworks((Interface, Interface) link)
        => link.Item1.Addresses
            .SelectMany(_ => link.Item2.Addresses, (address, neighbourAddress) => (address, neighbourAddress))
            .Where(t => t.address.NetworkAddress.Equals(t.neighbourAddress.NetworkAddress)
                        && !t.address.IpAddress.Equals(t.neighbourAddress.IpAddress));
}