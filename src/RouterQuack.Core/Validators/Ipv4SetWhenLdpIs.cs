using RouterQuack.Core.Extensions;

namespace RouterQuack.Core.Validators;

/// <summary>
/// Generate an error if there are ASes in which LDP is used but IPv4 isn't enabled.
/// </summary>
public class Ipv4SetWhenLdpIs(ILogger<Ipv4SetWhenLdpIs> logger, Context context) : IValidator
{
    public string BeginMessage => "Ensuring IPv4 is enabled when LDP is";
    public ILogger Logger { get; } = logger;
    public Context Context { get; } = context;


    public void Validate()
    {
        var asses = Context.Asses
            .Where(a => a.Core == CoreType.LDP && !a.AddressFamily.HasFlag(IpVersion.IPv4));

        foreach (var @as in asses)
            this.Log(@as, "LDP is enabled, yet IPv4 isn't.");
    }
}