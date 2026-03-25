using RouterQuack.Core.Utils;

namespace RouterQuack.Core.Processors;

public class ToggleIbgp(
    ILogger<ToggleIbgp> logger,
    Context context,
    RouterUtils routerUtils) : IProcessor
{
    public string BeginMessage => "Toggling iBGP for configured routers";
    public ILogger Logger { get; } = logger;
    public Context Context { get; } = context;

    public void Process()
    {
        foreach (var router in Context.Asses.SelectMany(a => a.Routers))
            if (router.ParentAs.Igp == IgpType.iBGP)
                router.Bgp.Ibgp = true;
    }
}