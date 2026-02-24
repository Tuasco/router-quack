using System.Net;
using RouterQuack.Core.Utils;

namespace RouterQuack.Tests.Unit.Core.Utils;

public class NetworkUtilsTests
{
    private readonly NetworkUtils _utils = new();

    [Test]
    [Arguments("192.168.1.1/24", "192.168.1.1", 24)]
    [Arguments("10.0.0.1/8", "10.0.0.1", 8)]
    [Arguments("172.16.0.1/16", "172.16.0.1", 16)]
    [Arguments("2001:db8::1/64", "2001:db8::1", 64)]
    [Arguments("fe80::1/128", "fe80::1", 128)]
    public async Task ParseIpAddress_ValidInput_ReturnsCorrectAddress(
        string input, string expectedIp, int expectedPrefix)
    {
        var result = _utils.ParseIpAddress(input);

        await Assert.That(result.IpAddress.ToString()).IsEqualTo(expectedIp);
        await Assert.That(result.NetworkAddress.PrefixLength).IsEqualTo(expectedPrefix);
    }

    [Test]
    [Arguments("192.168.1.1", "Couldn't translate IP address")]
    [Arguments("invalid", "Couldn't translate IP address")]
    [Arguments("192.168.1.1/abc", "Couldn't translate IP address (invalid mask)")]
    [Arguments("invalid/24", "Couldn't translate IP address (invalid IP)")]
    public async Task ParseIpAddress_InvalidInput_ThrowsArgumentException(string input, string expectedMessage)
    {
        await Assert.That(() => _utils.ParseIpAddress(input))
            .Throws<ArgumentException>()
            .WithMessageContaining(expectedMessage);
    }

    [Test]
    [Arguments(null, IpVersion.Ipv6)]
    [Arguments("ipv4", IpVersion.Ipv4)]
    [Arguments("IPv4", IpVersion.Ipv4)]
    [Arguments("ipv6", IpVersion.Ipv6)]
    [Arguments("IPv6", IpVersion.Ipv6)]
    [Arguments("both", IpVersion.Ipv4 | IpVersion.Ipv6)]
    [Arguments("dual", IpVersion.Ipv4 | IpVersion.Ipv6)]
    [Arguments("dual stack", IpVersion.Ipv4 | IpVersion.Ipv6)]
    [Arguments("dual-stack", IpVersion.Ipv4 | IpVersion.Ipv6)]
    [Arguments("dual_stack", IpVersion.Ipv4 | IpVersion.Ipv6)]
    public async Task ParseIpVersion_ValidInput_ReturnsCorrectVersion(string? input, IpVersion expected)
    {
        await Assert.That(() => _utils.ParseIpVersion(input))
            .IsEqualTo(expected);
    }

    [Test]
    [Arguments("invalid")]
    [Arguments("unknown")]
    public async Task ParseIpVersion_InvalidInput_ThrowsArgumentException(string input)
    {
        await Assert.That(() => _utils.ParseIpVersion(input))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task GenerateAvailableIpAddress_ReturnsUniqueAddresses()
    {
        var space = IPNetwork.Parse("192.168.0.0/24");
        var counter = UInt128.Zero;
        var used = new HashSet<IPAddress>();

        var ip1 = _utils.GenerateAvailableIpAddress(space, ref counter, used);
        var ip2 = _utils.GenerateAvailableIpAddress(space, ref counter, used);

        await Assert.That(ip1).IsNotEqualTo(ip2);
        await Assert.That(used).Count().IsEqualTo(2);
    }

    [Test]
    public async Task GenerateAvailableIpAddress_SkipsUsedAddresses()
    {
        var space = IPNetwork.Parse("192.168.0.0/24");
        var counter = UInt128.Zero;
        var reserved = IPAddress.Parse("192.168.0.0");
        var used = new HashSet<IPAddress> { reserved };

        var ip = _utils.GenerateAvailableIpAddress(space, ref counter, used);

        await Assert.That(ip).IsNotEqualTo(reserved);
        await Assert.That(used).Count().IsEqualTo(2);
    }

    [Test]
    [Arguments("192.168.0.0/32", 2)]
    [Arguments("192.168.0.0/31", 3)]
    [Arguments("2001:db8::/128", 2)]
    [Arguments("2001:db8::/126", 5)]
    public async Task GenerateAvailableIpAddress_Overflow_ThrowsInvalidOperationException(string space, int iterations)
    {
        var network = IPNetwork.Parse(space);
        var counter = UInt128.Zero;
        var used = new HashSet<IPAddress>();

        foreach (var _ in Enumerable.Range(0, iterations - 1))
            _utils.GenerateAvailableIpAddress(network, ref counter, used);

        await Assert.That(() => _utils.GenerateAvailableIpAddress(network, ref counter, used))
            .Throws<InvalidOperationException>();
    }
}