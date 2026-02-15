using System.Diagnostics.Contracts;
using System.Net;
using RouterQuack.Core.Models;

namespace RouterQuack.Core.Utils;

public interface INetworkUtils
{
    /// <param name="ip">An IP address (string format).</param>
    /// <returns>A formatted IP address.</returns>
    /// <remarks>The IPv4 or IPv6 address is expected to include the prefix length in CIDR format.</remarks>
    /// <exception cref="ArgumentException">Invalid IP address.</exception>
    [Pure]
    public Address ParseIpAddress(string ip);

    /// <param name="igp">An IGP (string format).</param>
    /// <returns>The corresponding IGP (Enum format).</returns>
    /// <exception cref="ArgumentException">Non <c>null</c> and unknown IGP.</exception>
    [Pure]
    public IgpType ParseIgp(string? igp);

    /// <param name="bgp">A BGP relationship (string format).</param>
    /// <returns>The corresponding BGP relationship (Enum format).</returns>
    /// <exception cref="ArgumentException">Non <c>null</c> and unknown BGP relationship.</exception>
    [Pure]
    public BgpRelationship ParseBgp(string? bgp);

    /// <param name="version">An IP version (string format).</param>
    /// <returns>The corresponding IP version (Enum flags format).</returns>
    /// /// <exception cref="ArgumentException">Non <c>null</c> and unknown networks IP version.</exception>
    [Pure]
    public IpVersion ParseIpVersion(string? version);

    /// <summary>
    /// Return whether an interface has a common network with its neighbour.
    /// </summary>
    /// <param name="interface"></param>
    /// <returns><c>true</c> if the interfaces of the link share a common network.</returns>
    [Pure]
    public bool HasLinkNetwork(Interface @interface);
}

public class NetworkUtils : INetworkUtils
{
    public Address ParseIpAddress(string ip)
    {
        var parts = ip.Split('/');

        if (parts.Length != 2)
            throw new ArgumentException("Couldn't translate IP address");

        if (!int.TryParse(parts[1], out var mask))
            throw new ArgumentException("Couldn't translate IP address (invalid mask)");

        if (!IPAddress.TryParse(parts[0], out var ipAddress))
            throw new ArgumentException("Couldn't translate IP address (invalid IP)");

        return new(new(ipAddress, mask), ipAddress);
    }

    public IgpType ParseIgp(string? igp)
    {
        if (igp == null)
            return IgpType.Ibgp;

        return Enum.TryParse<IgpType>(igp, true, out var igpType)
            ? igpType
            : throw new ArgumentException("Couldn't parse IGP");
    }

    public BgpRelationship ParseBgp(string? bgp)
    {
        if (bgp == null)
            return BgpRelationship.None;

        return Enum.TryParse<BgpRelationship>(bgp, true, out var bgpRelationship)
            ? bgpRelationship
            : throw new ArgumentException("Couldn't parse BGP relationship");
    }

    public IpVersion ParseIpVersion(string? version)
    {
        if (version == null)
            return IpVersion.Ipv6;

        ReadOnlySpan<string> bothTokens = ["both", "dual", "dual stack", "dual_stack", "dual-stack"];
        version = version.ToLowerInvariant();

        if (bothTokens.Contains(version))
            return IpVersion.Ipv4 | IpVersion.Ipv6;

        return Enum.TryParse<IpVersion>(version, true, out var ipVersion)
            ? ipVersion
            : throw new ArgumentException("Couldn't parse IP version");
    }

    public bool HasLinkNetwork(Interface @interface)
        => (from address in @interface.Addresses
                from neighbourAddress in @interface.Neighbour!.Addresses
                where address.NetworkAddress.Equals(neighbourAddress.NetworkAddress)
                      && !address.IpAddress.Equals(neighbourAddress.IpAddress)
                select true)
            .Any();
}