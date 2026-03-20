using System.Net;
using RouterQuack.Core.Extensions;
using RouterQuack.Core.Utils;

namespace RouterQuack.Core.Processors;

public class GenerateLoopbackAddresses(
    ILogger<GenerateLoopbackAddresses> logger,
    Context context,
    NetworkUtils networkUtils) : IProcessor
{
    public string BeginMessage => "Generating loopback addresses for routers";
    public ILogger Logger { get; } = logger;
    public Context Context { get; } = context;

    private UInt128 _addressCounter;
    private HashSet<IPAddress> _usedAddresses = null!;

    public void Process()
    {
        GenerateV4LoopbackAddresses();
        GenerateV6LoopbackAddresses();
    }

    private void GenerateV4LoopbackAddresses()
    {
        var routers = Context.Asses
            .Where(a => (a.IpVersions & IpVersion.IPv4) == IpVersion.IPv4)
            .SelectMany(a => a.Routers)
            .Where(r => r is { External: false, LoopbackAddressV4: null });

        _usedAddresses = Context.Asses
            .SelectMany(a => a.Routers)
            .Where(r => r.LoopbackAddressV4 != null)
            .Select(r => r.LoopbackAddressV4!)
            .ToHashSet();

        _addressCounter = 1;

        foreach (var router in routers)
        {
            if (!router.ParentAs.LoopbackSpaceV4.HasValue)
            {
                this.Log(router, "Couldn't generate loopback address (no loopback space defined in AS)");
                continue;
            }

            var space = router.ParentAs.LoopbackSpaceV4.Value;
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

            router.LoopbackAddressV4 = ip;
            logger.LogDebug("Generated loopback {IpAddress} for router {RouterName} in AS {AsNumber}",
                ip, router.Name, router.ParentAs.Number);
        }
    }

    private void GenerateV6LoopbackAddresses()
    {
        var routers = Context.Asses
            .Where(a => (a.IpVersions & IpVersion.IPv6) == IpVersion.IPv6)
            .SelectMany(a => a.Routers)
            .Where(r => r is { External: false, LoopbackAddressV6: null });

        _usedAddresses = Context.Asses
            .SelectMany(a => a.Routers)
            .Where(r => r.LoopbackAddressV6 != null)
            .Select(r => r.LoopbackAddressV6!)
            .ToHashSet();

        _addressCounter = 1;

        foreach (var router in routers)
        {
            if (!router.ParentAs.LoopbackSpaceV6.HasValue)
            {
                this.Log(router, "Couldn't generate loopback address (no loopback space defined in AS)");
                continue;
            }

            var space = router.ParentAs.LoopbackSpaceV6.Value;
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

            router.LoopbackAddressV6 = ip;
            logger.LogDebug("Generated loopback {IpAddress} for router {RouterName} in AS {AsNumber}",
                ip, router.Name, router.ParentAs.Number);
        }
    }
}