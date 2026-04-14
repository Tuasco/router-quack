using System.Net.Sockets;
using RouterQuack.Core.Extensions;

namespace RouterQuack.Core.Validators;

/// <summary>
/// Generate an error if a loopback space isn't provided when required, or is of the wrong address family
/// </summary>
public class ValidLoopbackSpaces(ILogger<ValidLoopbackSpaces> logger, Context context) : IValidator
{
    public string BeginMessage => "Ensuring loopback spaces are valid";
    public ILogger Logger { get; } = logger;
    public Context Context { get; } = context;


    public void Validate()
    {
        var assesV4 = Context.Asses
            .Where(a => !a.FullyExternal)
            .Where(a => a.AddressFamily.HasFlag(IpVersion.IPv4))
            .Where(a => a.Routers.Any(r => r.LoopbackAddressV4 is null));

        foreach (var @as in assesV4)
        {
            if (@as.LoopbackSpaceV4 is null)
                this.Log(@as, "Missing loopback_space_v4");
            else if (@as.LoopbackSpaceV4?.BaseAddress.AddressFamily != AddressFamily.InterNetwork)
                this.Log(@as, "Invalid loopback_space_v4");
        }

        var assesV6 = Context.Asses
            .Where(a => !a.FullyExternal)
            .Where(a => (a.AddressFamily & IpVersion.IPv6) == IpVersion.IPv6)
            .Where(a => a.Routers.Any(r => r.LoopbackAddressV6 is null));

        foreach (var @as in assesV6)
        {
            if (@as.LoopbackSpaceV6 is null)
                this.Log(@as, "Missing loopback_space_v6");
            else if (@as.LoopbackSpaceV6?.BaseAddress.AddressFamily != AddressFamily.InterNetworkV6)
                this.Log(@as, "Invalid loopback_space_v6");
        }
    }
}