using Microsoft.Extensions.Logging;
using RouterQuack.Core.Extensions;
using RouterQuack.Core.Models;

namespace RouterQuack.Core.Validators;

/// <summary>
/// Generate an error if there are duplicate router names.
/// </summary>
public class NoDuplicateRouterNames(ILogger<NoDuplicateRouterNames> logger) : IValidator
{
    public bool ErrorsOccurred { get; set; }
    public ILogger Logger { get; set; } = logger;

    public void Validate(ICollection<As> asses)
    {
        var routers = asses
            .SelectMany(a => a.Routers)
            .CountBy(n => n.Name)
            .Where(c => c.Value > 1)
            .Select(i => i.Key);

        foreach (var router in routers)
            this.LogError("Duplicate routers with same name \"{RouterName}\".", router);
    }
}