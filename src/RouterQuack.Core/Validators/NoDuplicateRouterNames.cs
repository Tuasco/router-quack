using RouterQuack.Core.Extensions;

namespace RouterQuack.Core.Validators;

/// <summary>
/// Generate an error if there are duplicate router names.
/// </summary>
public class NoDuplicateRouterNames(ILogger<NoDuplicateRouterNames> logger, Context context) : IValidator
{
    public bool ErrorsOccurred { get; set; }
    public string BeginMessage => "Ensuring no routers with a common name exist";
    public ILogger Logger { get; } = logger;
    public Context Context { get; } = context;


    public void Validate()
    {
        var routers = Context.Asses
            .SelectMany(a => a.Routers)
            .CountBy(n => n.Name)
            .Where(c => c.Value > 1)
            .Select(i => i.Key);

        foreach (var router in routers)
            this.LogError("Duplicate routers with same name \"{RouterName}\".", router);
    }
}