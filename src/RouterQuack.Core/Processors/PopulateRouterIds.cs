using RouterQuack.Core.Utils;

namespace RouterQuack.Core.Processors;

/// <summary>
/// Populate router non-configured IDs with IPv4 loopback address, or a default fallback ID from the router's name.
/// </summary>
public class PopulateRouterIds(
    ILogger<PopulateRouterIds> logger,
    Context context,
    RouterUtils routerUtils) : IProcessor
{
    public string BeginMessage => "Populating router IDs from IPv4 loopback addresses (or generated)";
    public ILogger Logger { get; } = logger;
    public Context Context { get; } = context;

    public void Process()
    {
        foreach (var router in Context.Asses.SelectMany(a => a.Routers))
            router.Id ??= router.LoopbackAddressV4 ?? routerUtils.GetDefaultId(router.Name);
    }
}