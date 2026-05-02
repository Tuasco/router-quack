using RouterQuack.Core.Extensions;

namespace RouterQuack.Core.Validators;

/// <summary>
/// Generate an error if there are duplicate IP Addresses.
/// </summary>
public class NoDuplicateIpAddress(ILogger<NoDuplicateIpAddress> logger, Context context) : IValidator
{
    public string BeginMessage => "Ensuring no duplicate manual IP address exist";
    public ILogger Logger { get; } = logger;
    public Context Context { get; } = context;


    public void Validate()
    {
        var vrfNeighbourInterfaces = Context.Asses
            .SelectMany(a => a.Routers)
            .SelectMany(r => r.Interfaces)
            .Where(i => !string.IsNullOrEmpty(i.Vrf) && i.Neighbour != null)
            .Select(i => i.Neighbour!)
            .ToHashSet();

        var addresses = Context.Asses
            .SelectMany(a => a.Routers)
            .SelectMany(r => r.Interfaces)
            .Where(i => string.IsNullOrEmpty(i.Vrf) && !vrfNeighbourInterfaces.Contains(i))
            .SelectMany(i => i.Addresses)
            .CountBy(a => a.IpAddress)
            .Where(c => c.Value > 1)
            .Select(i => i.Key);

        foreach (var address in addresses)
            this.LogError("Duplicate addresses \"{IpAddress}\".", address);
    }
}