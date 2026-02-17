using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using RouterQuack.Core.Extensions;
using RouterQuack.Core.Models;

namespace RouterQuack.Core.Validators;

/// <summary>
/// Generate an error if there is a mismatch in the configured networks version per AS
/// </summary>
public class ValidNetworkSpaces(ILogger<ValidNetworkSpaces> logger) : IValidator
{
    public bool ErrorsOccurred { get; set; }
    public string? BeginMessage { get; init; } = null;
    public ILogger Logger { get; set; } = logger;

    public void Validate(ICollection<As> asses)
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
}