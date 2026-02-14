using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using RouterQuack.Core.Extensions;
using RouterQuack.Core.Models;

namespace RouterQuack.Core.Steps;

public class Step3GenerateIpAddresses(ILogger<Step2RunChecks> logger) : IStep
{
    public bool ErrorsOccurred { get; set; }
    public ILogger Logger { get; set; } = logger;

    private UInt128 _addressCount;
    private ICollection<IPAddress> _usedAddresses = null!;

    public void Execute(ICollection<As> asses)
    {
        _usedAddresses = asses
            .SelectMany(a => a.Routers)
            .SelectMany(r => r.Interfaces)
            .SelectMany(i => i.Addresses)
            .Select(a => a.IpAddress)
            .ToList();

        foreach (var @as in asses)
        {
            if (@as.FullyExternal)
                continue;

            var links = @as.Routers.SelectMany(r => r.Interfaces)
                .Where(i => !i.Addresses.Any())
                .Where(i =>
                {
                    // Enforce a consistent ordering to pick the link only once
                    // If the neighbour has an address, this interface is the only candidate
                    if (i.Neighbour!.Addresses.Any())
                        return true;

                    return string.Compare(i.Name, i.Neighbour.Name, StringComparison.Ordinal) < 0;
                })
                .Select(i => new Tuple<Interface, Interface>(i, i.Neighbour!))
                .ToArray();

            _addressCount = 0;
            if ((@as.NetworksIpVersion & IpVersion.Ipv4) == IpVersion.Ipv4)
                foreach (var link in links)
                    AssignIpAddress(link, IpVersion.Ipv4);

            _addressCount = 0;
            // ReSharper disable once InvertIf
            if ((@as.NetworksIpVersion & IpVersion.Ipv6) == IpVersion.Ipv6)
                foreach (var link in links)
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
        IPNetwork? space = ipVersion switch
        {
            IpVersion.Ipv4 =>
                link.Item1.ParentRouter.ParentAs.NetworksSpaceV4
                ?? link.Item2.ParentRouter.ParentAs.NetworksSpaceV4!.Value,

            IpVersion.Ipv6 =>
                link.Item1.ParentRouter.ParentAs.NetworksSpaceV6
                ?? link.Item2.ParentRouter.ParentAs.NetworksSpaceV6!.Value,

            _ => null
        };

        if (space is null)
        {
            this.LogError("Interface {InterfaceName} of router {RouterName} in AS number {AsNumber} and " +
                          "its neighbour need automatic IP address, yet no networks space is defined in their AS(s).",
                link.Item1.Name, link.Item1.ParentRouter.Name, link.Item1.ParentRouter.ParentAs.Number);
            return;
        }

        // About to generate a second IPv4 address
        if (space.Value.BaseAddress.AddressFamily == AddressFamily.InterNetwork
            && (link.Item1.Addresses.Any(a => a.IpAddress.AddressFamily == AddressFamily.InterNetwork)
                || link.Item2.Addresses.Any(a => a.IpAddress.AddressFamily == AddressFamily.InterNetwork)))
        {
            this.LogError("Interface {InterfaceName} of router {RouterName} in AS number {AsNumber} or " +
                          "its neighbour already have an IPv4 address.",
                link.Item1.Name, link.Item1.ParentRouter.Name, link.Item1.ParentRouter.ParentAs.Number);

            return;
        }

        var maxBits = space.Value.BaseAddress.AddressFamily == AddressFamily.InterNetworkV6 ? 128 : 32;
        IPAddress ip1, ip2;
        try
        {
            ip1 = GenerateAvailableIpAddress(space.Value, maxBits);
            ip2 = GenerateAvailableIpAddress(space.Value, maxBits);
        }
        catch (InvalidOperationException)
        {
            this.LogError("Networks space of AS {AsNumber} has overflowed.", link.Item1.ParentRouter.ParentAs.Number);
            return;
        }

        var linkNetwork = new IPNetwork(ip1, maxBits - 1);
        link.Item1.Addresses.Add(new(linkNetwork, ip1));
        link.Item2.Addresses.Add(new(linkNetwork, ip2));
    }

    /// <summary>
    /// Generate a unique IP Address and add it to used addresses.
    /// </summary>
    /// <param name="space">The address space to pick networks from.</param>
    /// <param name="maxBits">Maximum bits the IP Address can take.</param>
    /// <returns>A new unique IP address.</returns>
    /// <exception cref="InvalidOperationException">Overflow of <paramref name="space"/>.</exception>
    private IPAddress GenerateAvailableIpAddress(IPNetwork space, int maxBits)
    {
        var hostBits = maxBits - space.PrefixLength;
        var baseBytes = space.BaseAddress.GetAddressBytes();
        IPAddress ip;

        do
        {
            // Ensure the requested offset doesn't exceed the subnet size
            if (_addressCount >= (UInt128)1 << hostBits)
                throw new InvalidOperationException("Required host bits overflow the subnet bits.");

            ip = ApplyOffset(baseBytes, _addressCount);
            _addressCount++;
        } while (_usedAddresses.Contains(ip));

        _usedAddresses.Add(ip);
        return ip;
    }

    /// <summary>
    /// Creates a new <see cref="IPAddress"/> by adding a numerical offset to a base byte array.
    /// </summary>
    /// <remarks>
    /// This method treats the byte array as a big-endian integer (network byte order). 
    /// It iterates from the least significant byte (right) to the most significant byte (left),
    /// applying a carry-over logic similar to how an odometer increments.
    /// </remarks>
    /// <param name="baseBytes">The network-order byte representation of the starting IP address.</param>
    /// <param name="offset">The number of addresses to increment from the base address.</param>
    /// <returns>A new <see cref="IPAddress"/> representing the incremented value.</returns>
    private static IPAddress ApplyOffset(byte[] baseBytes, UInt128 offset)
    {
        var result = (byte[])baseBytes.Clone();
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