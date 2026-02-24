using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Net;

namespace RouterQuack.Core.Models;

public sealed class Address
{
    /// <param name="address">An IP address (string format).</param>
    /// <returns>A formatted IP address.</returns>
    /// <remarks>The IPv4 or IPv6 address is expected to include the prefix length in CIDR format.</remarks>
    /// <exception cref="ArgumentException">Invalid IP address.</exception>
    [SetsRequiredMembers]
    public Address(string address)
    {
        var parts = address.Split('/');

        if (parts.Length != 2)
            throw new ArgumentException($"Couldn't translate IP address {address} (invalid format)");

        if (!int.TryParse(parts[1], out var mask))
            throw new ArgumentException($"Couldn't translate IP address {address} (invalid mask)");

        if (!IPAddress.TryParse(parts[0], out var ipAddress))
            throw new ArgumentException($"Couldn't translate IP address {address} (invalid IP)");

        IpAddress = ipAddress;
        NetworkAddress = new(ipAddress, mask);
    }

    [SetsRequiredMembers]
    public Address(IPNetwork networkAddress, IPAddress ipAddress)
        => (NetworkAddress, IpAddress) = (networkAddress, ipAddress);

    public required IPNetwork NetworkAddress { get; init; }

    public required IPAddress IpAddress { get; init; }

    [Pure]
    public override string ToString()
    {
        return $"{IpAddress.ToString()}/{NetworkAddress.PrefixLength}";
    }
}