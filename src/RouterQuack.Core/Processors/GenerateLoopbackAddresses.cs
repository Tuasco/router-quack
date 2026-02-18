using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using RouterQuack.Core.Extensions;
using RouterQuack.Core.Models;
using RouterQuack.Core.Utils;

namespace RouterQuack.Core.Processors;

public class GenerateLoopbackAddresses(
    ILogger<GenerateLoopbackAddresses> logger,
    Context context,
    NetworkUtils networkUtils) : IProcessor
{
    public bool ErrorsOccurred { get; set; }
    public string BeginMessage => "Generating loopback addresses for routers";
    public ILogger Logger { get; } = logger;
    public Context Context { get; } = context;

    private UInt128 _addressCounter;
    private List<IPAddress> _usedAddresses = null!;

    public void Process()
    {
        var routers = Context.Asses
            .SelectMany(a => a.Routers)
            .Where(r => r is { External: false, LoopbackAddress: null });

        _usedAddresses = Context.Asses
            .SelectMany(a => a.Routers)
            .Where(r => r.LoopbackAddress != null)
            .Select(r => r.LoopbackAddress!.IpAddress)
            .ToList();

        foreach (var router in routers)
        {
            if (!router.ParentAs.LoopbackSpace.HasValue)
            {
                this.LogError("Need to generate  loopback address for {RouterName}, yet no space was provided in AS.",
                    router.Name);
                continue;
            }

            var space = router.ParentAs.LoopbackSpace.Value;
            var maxBits = space.BaseAddress.AddressFamily == AddressFamily.InterNetworkV6 ? 128 : 32;
            IPAddress ip;
            try
            {
                ip = networkUtils.GenerateAvailableIpAddress(space, ref _addressCounter, _usedAddresses);
            }
            catch (InvalidOperationException)
            {
                this.LogError("Loopback space of AS {AsNumber} has overflowed.", router.ParentAs.Number);
                return;
            }

            router.LoopbackAddress = new(new(ip, maxBits), ip);
        }
    }
}