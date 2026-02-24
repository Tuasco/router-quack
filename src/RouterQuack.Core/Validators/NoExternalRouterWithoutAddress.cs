using RouterQuack.Core.Extensions;

namespace RouterQuack.Core.Validators;

/// <summary>
/// Generate an error if there are external routers with no manual IP Addresses.
/// </summary>
public class NoExternalRouterWithoutAddress(
    ILogger<NoExternalRouterWithoutAddress> logger,
    Context context) : IValidator
{
    public bool ErrorsOccurred { get; set; }
    public string BeginMessage => "Ensuring no external routers lack an IP address";
    public ILogger Logger { get; } = logger;
    public Context Context { get; } = context;


    public void Validate()
    {
        var interfaces = Context.Asses
            .SelectMany(a => a.Routers)
            .Where(r => r.External)
            .SelectMany(r => r.Interfaces)
            .Where(i => !i.Addresses.Any() || !i.Neighbour!.Addresses.Any())
            .ToArray();

        foreach (var @interface in interfaces)
            this.Log(@interface, "Self or neighbour is marked external but has no configured IP address");
    }
}