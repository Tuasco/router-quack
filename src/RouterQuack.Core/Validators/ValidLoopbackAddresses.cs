using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using RouterQuack.Core.Extensions;
using RouterQuack.Core.Models;

namespace RouterQuack.Core.Validators;

/// <summary>
/// Generate an error if a loopback address is not in /128 (or /32 in IPv4)
/// </summary>
public class ValidLoopbackAddresses(ILogger<ValidLoopbackAddresses> logger, Context context) : IValidator
{
    public bool ErrorsOccurred { get; set; }
    public string? BeginMessage => null;
    public ILogger Logger { get; } = logger;
    public Context Context { get; } = context;


    public void Validate()
    {
        var routers = Context.Asses
            .SelectMany(a => a.Routers)
            .Where(r => r.LoopbackAddress is not null);

        foreach (var router in routers)
        {
            var maxBits = router.LoopbackAddress!.IpAddress.AddressFamily == AddressFamily.InterNetworkV6 ? 128 : 32;
            if (router.LoopbackAddress.NetworkAddress.PrefixLength == maxBits)
                continue;

            this.LogError("Invalid loopback address in router {RouterName} in AS number {AsNumber}",
                router.Name,
                router.ParentAs.Number);
        }
    }
}