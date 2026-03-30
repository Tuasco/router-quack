using RouterQuack.Core.Extensions;

namespace RouterQuack.Core.Validators;

/// <summary>
/// Throw a warning if custom additional configs are used.
/// </summary>
public class WarningWhenAdditionalConfig(ILogger<WarningWhenAdditionalConfig> logger, Context context) : IValidator
{
    public string BeginMessage => "Looking for additional configs";
    public ILogger Logger { get; } = logger;
    public Context Context { get; } = context;


    public void Validate()
    {
        var additionalConfigs = Context.Asses
            .SelectMany(a => a.Routers)
            .Any(r => r.AdditionalConfig != null || r.Interfaces.Any(i => i.AdditionalConfig != null));

        if (!additionalConfigs)
            return;

        this.LogWarning("Additional configs are used. Please make sure to test them before deploying.");
    }
}