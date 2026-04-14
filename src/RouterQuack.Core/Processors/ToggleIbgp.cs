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
            if (router.ParentAs.Core == CoreType.iBGP || router.BorderRouter)
                router.Bgp.Ibgp = true;
    }
}