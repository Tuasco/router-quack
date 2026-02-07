using System.Diagnostics.Contracts;
using Microsoft.Extensions.Logging;
using RouterQuack.Core.Models;

namespace RouterQuack.Core.Steps;

/// <summary>
/// Resolve the neighbours of the interfaces from their initial dummy neighbour.
/// </summary>
public class Step1ResolveNeighbours(ILogger<Step2RunChecks> logger) : IStep
{
    public bool ErrorsOccurred { get; set; }


    public void Execute(ICollection<As> asses)
    {
        var interfaces = asses
            .SelectMany(a => a.Routers)
            .SelectMany(a => a.Interfaces);

        foreach (var @interface in interfaces)
            ReplaceNeighbour(asses, @interface);
    }

    /// <summary>
    /// Resolve and replace the neighbour of an interface.
    /// </summary>
    /// <param name="asses"></param>
    /// <param name="interface">
    /// The interface the neighbour of to resolve. Must be an indirect child of an As in <paramref name="asses"/>.
    /// </param>
    /// <remarks><paramref name="interface"/> must be an indirect child of an As in <paramref name="asses"/>.</remarks>
    /// <remarks>Will generate an error if the neighbour is in an unknown AS or incorrectly formatted.</remarks>
    private void ReplaceNeighbour(ICollection<As> asses, Interface @interface)
    {
        var neighbourPath = @interface.Neighbour?.Name.Split(':') ?? [];
        var routerName = neighbourPath.Last();
        var asNumber = AsNumberFromNeighbourPath(neighbourPath, @interface);

        var neighbour = ResolveNeighbour(asses, @interface, asNumber, routerName);
        if (neighbour is null)
        {
            logger.LogError("Couldn't resolve neighbour of interface {InterfaceName} in router {RouterName} " +
                            "of AS number {AsNumber}.",
                @interface.Name,
                @interface.ParentRouter.Name,
                @interface.ParentRouter.ParentAs.Number);
            ErrorsOccurred = true;
            return;
        }

        @interface.Neighbour = neighbour;
    }

    /// <summary>
    /// Resolve the neighbour of an interface.
    /// </summary>
    /// <param name="asses"></param>
    /// <param name="interface">The interface the neighbour of to resolve.</param>
    /// <param name="asNumber">The AS number of <paramref name="interface"/>'s neighbour.</param>
    /// <param name="routerName">The router's name of <paramref name="interface"/>'s neighbour.</param>
    /// <returns>The resolved interface, or <c>null</c>.</returns>
    /// <remarks><paramref name="interface"/> must be an indirect child of an As in <paramref name="asses"/>.</remarks>
    [Pure]
    private Interface? ResolveNeighbour(ICollection<As> asses, Interface @interface, int asNumber, string routerName)
    {
        // Try and get our neighbour
        return asses.FirstOrDefault(a => a.Number == asNumber)
            ?.Routers.FirstOrDefault(r => r.Name == routerName)
            ?.Interfaces.FirstOrDefault(FilterNeighbours);

        // Filter in neighbours that don't have neighbours (dummy neighbour with our actual neighbour's name)
        // Or neighbours that do have neighbours, that happen to be ourselves
        bool FilterNeighbours(Interface i) =>
            (i.Neighbour!.Neighbour is null && i.Neighbour.Name.Split(':').Last() == @interface.ParentRouter.Name)
            || (i.Neighbour.Neighbour is not null && i.Neighbour == @interface);
    }

    /// <summary>
    /// Get AS number from split neighbour path (separator is ':')
    /// </summary>
    /// <param name="neighbourPath">Array representing the path</param>
    /// <param name="interface">The current interface</param>
    /// <returns>AS number if success, else 0</returns>
    [Pure]
    private int AsNumberFromNeighbourPath(string[] neighbourPath, Interface @interface)
        => neighbourPath.Length switch
        {
            1 => @interface.ParentRouter.ParentAs.Number,
            2 => int.TryParse(neighbourPath.First(), out var asNumber) ? asNumber : 0,
            _ => 0
        };
}