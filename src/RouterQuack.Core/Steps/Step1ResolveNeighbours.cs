using System.Diagnostics.Contracts;
using Microsoft.Extensions.Logging;
using RouterQuack.Core.Extensions;
using RouterQuack.Core.Models;

namespace RouterQuack.Core.Steps;

/// <summary>
/// Resolve the neighbours of the interfaces from their initial dummy neighbour.
/// </summary>
public class Step1ResolveNeighbours(ILogger<Step1ResolveNeighbours> logger) : IStep
{
    public bool ErrorsOccurred { get; set; }
    public ILogger Logger { get; set; } = logger;


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
            this.LogError("Couldn't resolve neighbour of interface {InterfaceName} in router {RouterName} " +
                          "of AS number {AsNumber}.",
                @interface.Name, @interface.ParentRouter.Name, @interface.ParentRouter.ParentAs.Number);
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
    /// <remarks>
    /// <paramref name="interface"/> must be an indirect child of an As in <paramref name="asses"/>. <br /><br />
    /// This function might return an interface with a <c>null</c> neighbour if it thinks the latter is the correct
    /// neighbour, but wasn't parsed successfully due to errors in its configuration
    /// </remarks>
    [Pure]
    private Interface? ResolveNeighbour(ICollection<As> asses, Interface @interface, int asNumber, string routerName)
    {
        // Try and get our neighbour
        return asses.FirstOrDefault(a => a.Number == asNumber)
            ?.Routers.FirstOrDefault(r => r.Name == routerName)
            ?.Interfaces.FirstOrDefault(FilterNeighbours);

        // Filter in interfaces that don't have neighbours (dummy neighbour with our actual neighbour's name)
        // Or neighbours that do have neighbours, that happen to be ourselves
        // In both cases, the neighbour is not null per se.
        // If nothing matches, try to filter in interfaces with null neighbours (that failed to resolve their neighbour)
        bool FilterNeighbours(Interface i) =>
            (i.Neighbour is not null && (
                (i.Neighbour.Neighbour is null && i.Neighbour.Name.Split(':').Last() == @interface.ParentRouter.Name)
                || (i.Neighbour.Neighbour is not null && i.Neighbour == @interface)))
            || FilterNeighboursWithErrors(i);

        // Filter in interfaces with null neighbours, but their router's name matches (and FilterNeighbours())
        // This is useful when our neighbour has been parsed first but unsuccessfully.
        // If a neighbour matched here, log a warning
        bool FilterNeighboursWithErrors(Interface i)
        {
            var result = i.Neighbour is null && i.ParentRouter.Name == routerName;

            if (result)
            {
                this.LogWarning("The neighbour of interface {InterfaceName} in router {RouterName} of AS number " +
                                "{AsNumber} was resolved by guessing the exact interface of {NeighbourRouterName} " +
                                "({NeighbourInterfaceName}).",
                    @interface.Name,
                    @interface.ParentRouter.Name,
                    @interface.ParentRouter.ParentAs.Number,
                    routerName,
                    i.Name);

                i.Neighbour = @interface;
            }

            return result;
        }
    }

    /// <summary>
    /// Get AS number from split neighbour path (separator is ':').
    /// </summary>
    /// <param name="neighbourPath">Array representing the path.</param>
    /// <param name="interface">The current interface.</param>
    /// <returns>AS number if success, else 0.</returns>
    [Pure]
    private static int AsNumberFromNeighbourPath(string[] neighbourPath, Interface @interface)
        => neighbourPath.Length switch
        {
            1 => @interface.ParentRouter.ParentAs.Number,
            2 => int.TryParse(neighbourPath.First(), out var asNumber) ? asNumber : 0,
            _ => 0
        };
}