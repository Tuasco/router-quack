using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using RouterQuack.Core.Extensions;
using RouterQuack.Core.Models;

namespace RouterQuack.Core.Steps;

/// <summary>
/// Run various checks.
/// </summary>
public class Step2RunChecks(ILogger<Step2RunChecks> logger) : IStep
{
    public bool ErrorsOccurred { get; set; }
    public ILogger Logger { get; set; } = logger;

    public void Execute(ICollection<As> asses)
    {
        NoDuplicateRouterNames(asses);
        NoDuplicateIpAddress(asses);
        ValidBgpRelationships(asses);
        NoExternalWithoutAddress(asses);
        ValidNetworkSpaces(asses);
        ValidLoopbackAddresses(asses);
    }

    /// <summary>
    /// Generate an error if there are duplicate router names.
    /// </summary>
    /// <param name="asses"></param>
    private void NoDuplicateRouterNames(ICollection<As> asses)
    {
        var routers = asses
            .SelectMany(a => a.Routers)
            .CountBy(n => n.Name)
            .Where(c => c.Value > 1)
            .Select(i => i.Key);

        foreach (var router in routers)
            this.LogError("Duplicate routers with same name \"{RouterName}\"", router);
    }

    /// <summary>
    /// Generate an error if there are duplicate IP Addresses.
    /// </summary>
    /// <param name="asses"></param>
    private void NoDuplicateIpAddress(ICollection<As> asses)
    {
        var addresses = asses
            .SelectMany(a => a.Routers)
            .SelectMany(r => r.Interfaces)
            .SelectMany(i => i.Addresses)
            .CountBy(a => a.IpAddress)
            .Where(c => c.Value > 1)
            .Select(i => i.Key);

        foreach (var address in addresses)
            this.LogError("Duplicate addresses \"{IpAddress}\"", address);
    }

    /// <summary>
    /// Generate an error if there are uncoherent BGP relationships.
    /// </summary>
    /// <param name="asses"></param>
    /// <remarks>Will generate a warning if there are interfaces with an inter-AS neighbour but no BGP.</remarks>
    private void ValidBgpRelationships(ICollection<As> asses)
    {
        var interfaces = asses
            .SelectMany(a => a.Routers)
            .SelectMany(r => r.Interfaces)
            .ToList();

        while (interfaces.Count > 0)
        {
            var @interface = interfaces.First();

            // Check if our neighbour's BGP strategy matches ours
            if (@interface
                is not ({ Bgp: BgpRelationship.None, Neighbour.Bgp: BgpRelationship.None }
                or { Bgp: BgpRelationship.Peer, Neighbour.Bgp: BgpRelationship.Peer }
                or { Bgp: BgpRelationship.Client, Neighbour.Bgp: BgpRelationship.Provider }
                or { Bgp: BgpRelationship.Provider, Neighbour.Bgp: BgpRelationship.Client }))
                this.LogError("Invalid BGP relationship between interface {InterfaceName} of router {RouterName} " +
                              "in AS number {AsNumber}",
                    @interface.Name,
                    @interface.ParentRouter.Name,
                    @interface.ParentRouter.ParentAs.Number);

            // Check if BGP is on if our neighbour is in a different AS
            if (@interface.Bgp == BgpRelationship.None
                && @interface.ParentRouter.ParentAs.Number != @interface.Neighbour!.ParentRouter.ParentAs.Number)
                this.LogWarning(
                    "Interface {InterfaceName} of router {RouterName} in AS number {AsNumber} " +
                    "has a neighbour in another interface, yet doesn't use BGP",
                    @interface.Name,
                    @interface.ParentRouter.Name,
                    @interface.ParentRouter.ParentAs.Number);

            interfaces.Remove(@interface);
            interfaces.Remove(@interface.Neighbour!);
        }
    }

    /// <summary>
    /// Generate an error if there are external routers with no manual IP Addresses.
    /// </summary>
    /// <param name="asses"></param>
    private void NoExternalWithoutAddress(ICollection<As> asses)
    {
        var interfaces = asses
            .SelectMany(a => a.Routers)
            .Where(r => r.External)
            .SelectMany(r => r.Interfaces)
            .Where(i => !i.Addresses.Any() || !i.Neighbour!.Addresses.Any())
            .ToArray();

        foreach (var @interface in interfaces)
            this.LogError("Interface {InterfaceName} of router {RouterName} in AS number {AsNumber} " +
                          "or its neighbour is marked external but has no configured IP address",
                @interface.Name,
                @interface.ParentRouter.Name,
                @interface.ParentRouter.ParentAs.Number);
    }

    /// <summary>
    /// Generate an error if there is a mismatch in the configured networks version per AS
    /// </summary>
    /// <param name="asses"></param>
    private void ValidNetworkSpaces(ICollection<As> asses)
    {
        foreach (var @as in asses)
        {
            if (@as.NetworksSpaceV4 is not null
                && @as.NetworksSpaceV4?.BaseAddress.AddressFamily != AddressFamily.InterNetwork)
                this.LogError("Invalid networks space v4 address in AS number {AsNumber}", @as.Number);

            if (@as.NetworksSpaceV6 is not null
                && @as.NetworksSpaceV6?.BaseAddress.AddressFamily != AddressFamily.InterNetworkV6)
                this.LogError("Invalid networks space v6 address in AS number {AsNumber}", @as.Number);

            if (!@as.FullyExternal
                && @as
                    is { NetworksSpaceV4: null, NetworksIpVersion: IpVersion.Ipv4 }
                    or { NetworksSpaceV6: null, NetworksIpVersion: IpVersion.Ipv6 })
                this.LogError("The chosen networks version doesn't have a provided space in AS number {AsNumber}",
                    @as.Number);
        }
    }

    /// <summary>
    /// Generate an error if a loopback address is not in /128 (or /32 in IPv4)
    /// </summary>
    /// <param name="asses"></param>
    private void ValidLoopbackAddresses(ICollection<As> asses)
    {
        var routers = asses
            .SelectMany(a => a.Routers)
            .Where(r => r.LoopbackAddress is not null);

        foreach (var router in routers)
        {
            var maxBits = router.LoopbackAddress!.IpAddress.AddressFamily == AddressFamily.InterNetworkV6 ? 128 : 32;
            if (router.LoopbackAddress.NetworkAddress.PrefixLength == maxBits)
                continue;

            this.LogError("Invalid loopback address in router {RouterName} in AS number {AsNumber}",
                router.Name,
                router.ParentAs.Number);
        }
    }
}