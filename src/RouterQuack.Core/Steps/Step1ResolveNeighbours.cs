using RouterQuack.Core.Models;

namespace RouterQuack.Core.Steps;

/// <summary>
/// Resolves the neighbours of the interfaces from their initial dummy neighbour.
/// </summary>
public class Step1ResolveNeighbours : BaseStep
{
    public override void Execute(ICollection<As> asses)
    {
        var interfaces = asses
            .SelectMany(a => a.Routers)
            .SelectMany(a => a.Interfaces);

        foreach (var @interface in interfaces)
            ReplaceNeighbour(asses, @interface);

        base.Execute(asses);
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

        var errorEnd =
            $" in interface {@interface.Name} of router {@interface.ParentRouter.Name} (AS number {@interface.ParentRouter.ParentAs.Number}).";

        var asNumber = neighbourPath.Length switch
        {
            1 => @interface.ParentRouter.ParentAs.Number,
            2 =>
                // Try and get AS number. If not in neighbourPath, default to ours
                int.TryParse(neighbourPath.First(), out var index)
                    ? index
                    : throw new IndexOutOfRangeException("Invalid AS number" + errorEnd),
            _ => throw new InvalidDataException("Invalid neighbour formater" + errorEnd)
        };

        @interface.Neighbour = ResolveNeighbour(asses, @interface, asNumber, routerName)
                               ?? throw new InvalidDataException("Invalid neighbour" + errorEnd);
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
}