using RouterQuack.Core.Extensions;

namespace RouterQuack.Core.Validators;

/// <summary>
/// Generate an error if there are duplicate loopback Addresses.
/// </summary>
public class NoDuplicateLoopbackAddress(ILogger<NoDuplicateLoopbackAddress> logger, Context context) : IValidator
{
    public string BeginMessage => "Ensuring no duplicate manual IP address exist";
    public ILogger Logger { get; } = logger;
    public Context Context { get; } = context;


    public void Validate()
    {
        foreach (var @as in Context.Asses)
        {
            var addresses = @as.Routers
                .Select(r => r.LoopbackAddressV4)
                .Concat(@as.Routers.Select(r => r.LoopbackAddressV6))
                .Where(a => a != null)
                .CountBy(a => a!)
                .Where(c => c.Value > 1)
                .Select(i => i.Key);

            foreach (var address in addresses)
                this.LogError("Duplicate loopback address \"{IpAddress}\".", address);
        }
    }
}