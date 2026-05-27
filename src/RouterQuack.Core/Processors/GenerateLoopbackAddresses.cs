using System.Net;
using RouterQuack.Core.Extensions;
using RouterQuack.Core.Utils;

namespace RouterQuack.Core.Processors;

/// <summary>
/// Generate loopback addresses from loopback space for selected IP versions (skip if already configured).
/// </summary>
public class GenerateLoopbackAddresses(
    ILogger<GenerateLoopbackAddresses> logger,
    Context context,
    NetworkUtils networkUtils) : IProcessor
{
    public string BeginMessage => "Generating loopback addresses for routers";
    public ILogger Logger { get; } = logger;
    public Context Context { get; } = context;


    public void Process()
    {
        foreach (var @as in Context.Asses)
        {
            GenerateV4LoopbackAddresses(@as);
            GenerateV6LoopbackAddresses(@as);
        }
    }

    private void GenerateV4LoopbackAddresses(As @as)
    {
        if (!@as.AddressFamily.HasFlag(IpVersion.IPv4))
            return;

        var routersToProcess = @as.Routers
            .Where(r => r is { External: false, LoopbackAddressV4: null })
            .ToArray();

        // No space provided
        if (routersToProcess.Any() && !routersToProcess[0].ParentAs.LoopbackSpaceV4.HasValue)
        {
            this.Log(@as, "Couldn't generate IPv4 loopback address (no loopback space defined in AS)");
            return;
        }

        var usedAddresses = @as.Routers
            .Where(r => r.LoopbackAddressV4 != null)
            .Select(r => r.LoopbackAddressV4!)
            .ToHashSet();

        UInt128 addressCounter = 1;

        foreach (var router in routersToProcess)
        {
            var space = router.ParentAs.LoopbackSpaceV4!.Value;
            IPAddress ip;
            try
            {
                ip = networkUtils.GenerateAvailableIpAddress(space, ref addressCounter, usedAddresses);
            }
            catch (InvalidOperationException)
            {
                this.Log(router.ParentAs, "Loopback space overflow");
                return;
            }

            router.LoopbackAddressV4 = ip;
            logger.LogDebug("Generated loopback {IpAddress} for router {RouterName} in AS {AsNumber}",
                ip, router.Name, router.ParentAs.Number);
        }
    }

    private void GenerateV6LoopbackAddresses(As @as)
    {
        if (!@as.AddressFamily.HasFlag(IpVersion.IPv6) || @as.Core.HasFlag(CoreType.LDP))
            return;

        var routersToProcess = @as.Routers
            .Where(r => r is { External: false, LoopbackAddressV6: null })
            .ToArray();

        // No space provided
        if (routersToProcess.Any() && !routersToProcess[0].ParentAs.LoopbackSpaceV6.HasValue)
        {
            this.Log(@as, "Couldn't generate IPv6 loopback address (no loopback space defined in AS)");
            return;
        }

        var usedAddresses = @as.Routers
            .Where(r => r.LoopbackAddressV6 != null)
            .Select(r => r.LoopbackAddressV6!)
            .ToHashSet();

        UInt128 addressCounter = 1;

        foreach (var router in routersToProcess)
        {
            var space = router.ParentAs.LoopbackSpaceV6!.Value;
            IPAddress ip;
            try
            {
                ip = networkUtils.GenerateAvailableIpAddress(space, ref addressCounter, usedAddresses);
            }
            catch (InvalidOperationException)
            {
                this.Log(router.ParentAs, "Loopback space overflow");
                return;
            }

            router.LoopbackAddressV6 = ip;
            logger.LogDebug("Generated loopback {IpAddress} for router {RouterName} in AS {AsNumber}",
                ip, router.Name, router.ParentAs.Number);
        }
    }
}