using RouterQuack.Core.Extensions;

namespace RouterQuack.Core.Processors;

/// <summary>
/// Populate non-configured interfaces' MTU with a default value.
/// </summary>
public class PopulateMtu(
    ILogger<PopulateMtu> logger,
    Context context) : IProcessor
{
    public string BeginMessage => "Choosing adequate MTU if not set";
    public ILogger Logger { get; } = logger;
    public Context Context { get; } = context;

    public void Process()
    {
        foreach (var @interface in Context.Asses.SelectMany(a => a.Routers).SelectMany(r => r.Interfaces))
        {
            // If Neighbour's MTU was set
            if (@interface.Neighbour!.Mtu.HasValue)
            {
                if (@interface.Mtu.HasValue && @interface.Mtu.Value == @interface.Neighbour.Mtu)
                {
                    @interface.Mtu = @interface.Neighbour.Mtu;
                    this.Log(@interface,
                        $"Set MTU to the same value as the neighbour's ({@interface.Neighbour.Mtu}).",
                        LogLevel.Debug);
                }
                else
                    this.Log(@interface, "Conflicting MTU values with neighbour.");

                continue;
            }

            // If using MPLS
            if (@interface.ParentRouter.ParentAs.Core.HasFlag(CoreType.LDP) &&
                @interface.AsNumber == @interface.Neighbour!.AsNumber)
                @interface.Mtu ??= 1524;
            else
                @interface.Mtu ??= 1500;
        }
    }
}