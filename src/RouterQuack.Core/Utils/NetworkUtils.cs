using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Sockets;
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

    /// <summary>
    /// Generate a unique <see cref="IPAddress"/> and add it to used addresses.
    /// </summary>
    /// <param name="space">The address space to pick networks from.</param>
    /// <param name="addressCount">A reference to a counter, used as an offset.</param>
    /// <param name="usedAddresses">A collection of unavailable IP addresses.</param>
    /// <returns>A new unique IP address.</returns>
    /// <exception cref="InvalidOperationException">Overflow of <paramref name="space"/>.</exception>
    public IPAddress GenerateAvailableIpAddress(IPNetwork space,
        ref UInt128 addressCount,
        ICollection<IPAddress> usedAddresses);
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

    public IPAddress GenerateAvailableIpAddress(IPNetwork space,
        ref UInt128 addressCount,
        ICollection<IPAddress> usedAddresses)
    {
        var maxBits = space.BaseAddress.AddressFamily == AddressFamily.InterNetworkV6 ? 128 : 32;
        var hostBits = maxBits - space.PrefixLength;
        var baseBytes = space.BaseAddress.GetAddressBytes();
        IPAddress ip;

        do
        {
            // Ensure the requested offset doesn't exceed the subnet size
            if (addressCount >= (UInt128)1 << hostBits)
                throw new InvalidOperationException("Required host bits overflow the subnet bits.");

            ip = ApplyOffset(baseBytes, addressCount);
            addressCount++;
        } while (usedAddresses.Contains(ip));

        usedAddresses.Add(ip);
        return ip;

        // Fill-in the host bytes of a new IPAddress and return it
        static IPAddress ApplyOffset(ReadOnlySpan<byte> baseBytes, UInt128 offset)
        {
            // Allocating on the stack instead of the heap by copying improves performance
            Span<byte> result = stackalloc byte[baseBytes.Length];
            baseBytes.CopyTo(result);

            var carry = offset;

            // Apply the offset from right to left (Big-endian)
            for (var i = result.Length - 1; i >= 0 && carry > 0; i--)
            {
                var val = result[i] + carry;
                result[i] = (byte)(val & 0xFF);
                carry = val >> 8;
            }

            return new(result);
        }
    }
}