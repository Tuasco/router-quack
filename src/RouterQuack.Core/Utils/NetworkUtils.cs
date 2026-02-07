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
}