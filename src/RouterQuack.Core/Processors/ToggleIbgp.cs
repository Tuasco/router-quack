namespace RouterQuack.Core.Processors;

/// <summary>
/// Enable iBGP on routers in an iBGP AS, or just using eBGP.
/// </summary>
public class ToggleIbgp(
    ILogger<ToggleIbgp> logger,
    Context context) : IProcessor
{
    public string BeginMessage => "Toggling iBGP for configured routers";
    public ILogger Logger { get; } = logger;
    public Context Context { get; } = context;

    public void Process()
    {
        foreach (var router in Context.Asses.SelectMany(a => a.Routers))
            // Only enable automatically if the router isn't a VPN client
            if (router.ParentAs.Core.HasFlag(CoreType.iBGP) ||
                router.BorderRouter && router.Interfaces.All(i => string.IsNullOrEmpty(i.Neighbour!.Vrf)))
                router.Bgp.Ibgp ??= true;
            else
                router.Bgp.Ibgp ??= false;
    }
}