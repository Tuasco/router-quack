namespace RouterQuack.Core.Processors;

/// <summary>
/// Enable/disable eBGP policies on routers if not set.
/// </summary>
public class ToggleBgpPolicies(ILogger<ToggleBgpPolicies> logger, Context context) : IProcessor
{
    public string BeginMessage => "Toggling eBGP policies for non-configured routers";
    public ILogger Logger { get; } = logger;
    public Context Context { get; } = context;

    public void Process()
    {
        foreach (var router in Context.Asses.SelectMany(a => a.Routers))
            // Only enable automatically if the router isn't a VPN client
            if (router.BorderRouter &&
                router.Interfaces.All(i => string.IsNullOrEmpty(i.Neighbour!.Vrf)))
                router.Bgp.Policies ??= true;
            else
                router.Bgp.Policies ??= false;
    }
}