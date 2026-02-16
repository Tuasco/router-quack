using Microsoft.Extensions.Logging;
using RouterQuack.Core.Extensions;
using RouterQuack.Core.Models;

namespace RouterQuack.Core.Validators;

/// <summary>
/// Generate an error if there are external routers with no manual IP Addresses.
/// </summary>
public class NoExternalRouterWithoutAddress(ILogger<NoExternalRouterWithoutAddress> logger) : IValidator
{
    public bool ErrorsOccurred { get; set; }
    public ILogger Logger { get; set; } = logger;

    public void Validate(ICollection<As> asses)
    {
        var interfaces = asses
            .SelectMany(a => a.Routers)
            .Where(r => r.External)
            .SelectMany(r => r.Interfaces)
            .Where(i => !i.Addresses.Any() || !i.Neighbour!.Addresses.Any())
            .ToArray();

        foreach (var @interface in interfaces)
            this.LogError("Interface {InterfaceName} of router {RouterName} in AS number {AsNumber} " +
                          "or its neighbour is marked external but has no configured IP address",
                @interface.Name,
                @interface.ParentRouter.Name,
                @interface.ParentRouter.ParentAs.Number);
    }
}