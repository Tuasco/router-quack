using System.Net.Sockets;
using RouterQuack.Core.Extensions;

namespace RouterQuack.Core.Validators;

/// <summary>
/// Generate an error if a loopback address is not in /128 (or /32 in IPv4) or is of the wrong address family
/// </summary>
public class ValidLoopbackAddresses(ILogger<ValidLoopbackAddresses> logger, Context context) : IValidator
{
    public string BeginMessage => "Ensuring loopback addresses are valid";
    public ILogger Logger { get; } = logger;
    public Context Context { get; } = context;


    public void Validate()
    {
        var routers = Context.Asses
            .SelectMany(a => a.Routers)
            .Where(r => r.LoopbackAddressV4 is not null || r.LoopbackAddressV6 is not null);

        foreach (var router in routers)
        {
            if (router.LoopbackAddressV4 is not null
                && router.LoopbackAddressV4?.AddressFamily != AddressFamily.InterNetwork)
                this.Log(router, "Invalid loopback_v4 address");

            if (router.LoopbackAddressV6 is not null
                && router.LoopbackAddressV6?.AddressFamily != AddressFamily.InterNetworkV6)
                this.Log(router, "Invalid loopback_v6 address");
        }
    }
}