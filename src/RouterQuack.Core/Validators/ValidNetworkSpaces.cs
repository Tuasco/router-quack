using System.Net.Sockets;
using RouterQuack.Core.Extensions;

namespace RouterQuack.Core.Validators;

/// <summary>
/// Generate an error if there is a mismatch in the configured networks version per AS
/// </summary>
public class ValidNetworkSpaces(ILogger<ValidNetworkSpaces> logger, Context context) : IValidator
{
    public bool ErrorsOccurred { get; set; }
    public string BeginMessage => "Ensuring network spaces are valid";
    public ILogger Logger { get; } = logger;
    public Context Context { get; } = context;

    private const IpVersion BothVersions = IpVersion.Ipv6 | IpVersion.Ipv4;

    public void Validate()
    {
        foreach (var @as in Context.Asses)
        {
            if (@as.NetworksSpaceV4 is not null
                && @as.NetworksSpaceV4?.BaseAddress.AddressFamily != AddressFamily.InterNetwork)
                this.Log(@as, "Invalid networks space v4");

            if (@as.NetworksSpaceV6 is not null
                && @as.NetworksSpaceV6?.BaseAddress.AddressFamily != AddressFamily.InterNetworkV6)
                this.Log(@as, "Invalid networks space v6");

            if (!@as.FullyExternal
                && @as
                    is { NetworksSpaceV4: null, NetworksIpVersion: IpVersion.Ipv4 or BothVersions }
                    or { NetworksSpaceV6: null, NetworksIpVersion: IpVersion.Ipv6 or BothVersions })
                this.Log(@as, "The chosen networks version doesn't have a provided space");
        }
    }
}