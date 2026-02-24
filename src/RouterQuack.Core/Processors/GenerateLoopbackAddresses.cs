using System.Net;
using System.Net.Sockets;
using RouterQuack.Core.Extensions;
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
    private HashSet<IPAddress> _usedAddresses = null!;

    public void Process()
    {
        var routers = Context.Asses
            .SelectMany(a => a.Routers)
            .Where(r => r is { External: false, LoopbackAddress: null });

        _usedAddresses = Context.Asses
            .SelectMany(a => a.Routers)
            .Where(r => r.LoopbackAddress != null)
            .Select(r => r.LoopbackAddress!.IpAddress)
            .ToHashSet();

        foreach (var router in routers)
        {
            if (!router.ParentAs.LoopbackSpace.HasValue)
            {
                this.Log(router, "Couldn't generate loopback address (no loopback space defined in AS)");
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
                this.Log(router.ParentAs, "Loopback space has overflowed");
                return;
            }

            router.LoopbackAddress = new(new(ip, maxBits), ip);
            logger.LogDebug("Generated loopback {IpNetwork} for router {RouterName} in AS {AsNumber}",
                new IPNetwork(ip, maxBits), router.Name, router.ParentAs.Number);
        }
    }
}