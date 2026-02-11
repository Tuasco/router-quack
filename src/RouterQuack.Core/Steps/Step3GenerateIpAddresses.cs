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

    private ulong _networkCount;
    private List<IPAddress> _networkAddresses = null!;

    public void Execute(ICollection<As> asses)
    {
        _networkAddresses = asses.SelectMany(a => a.Routers)
            .SelectMany(r => r.Interfaces)
            .SelectMany(i => i.Addresses)
            .Select(a => a.IpAddress)
            .ToList();

        foreach (var @as in asses)
        {
            _networkCount = 0;

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
                .Select(i => new Tuple<Interface, Interface>(i, i.Neighbour!));

            foreach (var link in links)
                AssignIpAddress(link);
        }
    }

    /// <summary>
    /// Assign an IP address to the interface and its neighbour.
    /// </summary>
    /// <param name="link">The interface to assign an IP address to.</param>
    private void AssignIpAddress(Tuple<Interface, Interface> link)
    {
        // No networks space whatsoever
        if (link is
            {
                Item1.ParentRouter.ParentAs.NetworksSpace: null,
                Item2.ParentRouter.ParentAs.NetworksSpace: null
            })
        {
            this.LogError("Interface {InterfaceName} of router {RouterName} in AS number {AsNumber} and " +
                          "its neighbour need automatic IP address, yet no networks space is defined in their AS(s).",
                link.Item1.Name, link.Item1.ParentRouter.Name, link.Item1.ParentRouter.ParentAs.Number);

            return;
        }

        var space = link.Item1.ParentRouter.ParentAs.NetworksSpace
                    ?? link.Item2.ParentRouter.ParentAs.NetworksSpace!.Value;

        // About to generate a second IPv4 address
        if (space.BaseAddress.AddressFamily == AddressFamily.InterNetwork
            && (link.Item1.Addresses.Any(a => a.IpAddress.AddressFamily == AddressFamily.InterNetwork)
                || link.Item2.Addresses.Any(a => a.IpAddress.AddressFamily == AddressFamily.InterNetwork)))
        {
            this.LogError("Interface {InterfaceName} of router {RouterName} in AS number {AsNumber} or " +
                          "its neighbour already have an IPv4 address.",
                link.Item1.Name, link.Item1.ParentRouter.Name, link.Item1.ParentRouter.ParentAs.Number);

            return;
        }

        Tuple<Address, Address> addresses;
        try
        {
            addresses = GenerateIpAddresses(space);
        }
        catch (InvalidOperationException)
        {
            this.LogError("Networks space of AS {AsNumber} has overflowed.", link.Item1.ParentRouter.ParentAs.Number);
            return;
        }

        link.Item1.Addresses.Add(addresses.Item1);
        link.Item2.Addresses.Add(addresses.Item2);
        _networkCount++;
    }

    /// <summary>
    /// Generate a pair of <see cref="Address"/> in a subspace from a given space.
    /// </summary>
    /// <param name="space">The address space to pick networks from.</param>
    /// <returns>A pair of <see cref="Address"/> objects representing the addresses of two interfaces.</returns>
    /// <exception cref="InvalidOperationException">Overflow of <paramref name="space"/>.</exception>
    private Tuple<Address, Address> GenerateIpAddresses(IPNetwork space)
    {
        var maxBits = space.BaseAddress.AddressFamily == AddressFamily.InterNetworkV6 ? 128 : 32;
        var hostBits = maxBits - space.PrefixLength;

        // Ensure the requested offset doesn't exceed the subnet size
        // If hostBits >= 64, a ulong offset will never overflow the subnet.
        if (hostBits < 64 && _networkCount * 2 + 1 >= 1UL << hostBits)
            throw new InvalidOperationException();

        var baseBytes = space.BaseAddress.GetAddressBytes();
        var ip1 = ApplyOffset(baseBytes, _networkCount * 2);
        var ip2 = ApplyOffset(baseBytes, _networkCount * 2 + 1);
        var linkNetwork = new IPNetwork(ip1, maxBits - 1);

        return new(new(linkNetwork, ip1), new(linkNetwork, ip2));
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
    private static IPAddress ApplyOffset(byte[] baseBytes, ulong offset)
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