using System.Diagnostics.Contracts;
using RouterQuack.Core.Extensions;
using RouterQuack.Core.Processors.Models;

namespace RouterQuack.Core.Processors;

/// <summary>
/// Resolve the neighbours of the interfaces from their initial dummy neighbour.
/// </summary>
/// <remarks>This step has to be executed first, even before validators.</remarks>
public class ResolveNeighbours(ILogger<ResolveNeighbours> logger, Context context) : IProcessor
{
    public string BeginMessage => "Resolving neighbours";
    public ILogger Logger { get; } = logger;
    public Context Context { get; } = context;

    public void Process()
    {
        var interfaces = Context.Asses
            .SelectMany(a => a.Routers)
            .SelectMany(a => a.Interfaces);

        foreach (var @interface in interfaces)
            ReplaceNeighbour(Context.Asses, @interface);
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
        // Already replaced
        if (@interface.Neighbour!.Neighbour is not null)
            return;

        var neighbourReference = new NeighbourReference(@interface.Neighbour!.Name);
        var neighbour = ResolveNeighbour(asses, @interface, neighbourReference);
        @interface.Neighbour = neighbour;
    }

    /// <summary>
    /// Resolve the neighbour of an interface.
    /// </summary>
    /// <param name="asses"></param>
    /// <param name="interface">The interface the neighbour of to resolve.</param>
    /// <param name="neighbourReference">Parsed neighbour path.</param>
    /// <returns>The resolved interface, or <c>null</c>.</returns>
    /// <remarks>
    /// <paramref name="interface"/> must be an indirect child of an As in <paramref name="asses"/>. <br /><br />
    /// This function might return an interface with a <c>null</c> neighbour if it thinks the latter is the correct
    /// neighbour, but wasn't parsed successfully due to errors in its configuration
    /// </remarks>
    [Pure]
    private Interface? ResolveNeighbour(ICollection<As> asses, Interface @interface,
        NeighbourReference neighbourReference)
    {
        /* For large scale, consider pre-building lookups for interfaces by (asNumber, routerName, interfaceName)
         * to avoid O(n) FirstOrDefault calls.
         * Using lookups would bring us down from O(R * I) to O(R + I).
         */
        var router = asses.FirstOrDefault(a => a.Number == neighbourReference.AsNumber)?
            .Routers.FirstOrDefault(r => r.Name == neighbourReference.RouterName);
        if (router is null)
        {
            this.Log(@interface, "Could not resolve neighbour");
            return null;
        }

        var candidates = router.Interfaces
            .Where(i => string.IsNullOrEmpty(neighbourReference.InterfaceName)
                        || i.Name == neighbourReference.InterfaceName).ToArray();
        if (TryGetResolvedCandidate(candidates, @interface, neighbourReference, out var resolvedCandidate))
            return resolvedCandidate;
        if (TryGetGuessedCandidate(candidates, @interface, neighbourReference, out var guessedCandidate))
            return guessedCandidate;

        this.Log(@interface, "Neighbour likely referenced more than once");
        return null;
    }

    /// <summary>
    /// Try to resolve a neighbour from candidates that already point back to the current interface.
    /// </summary>
    /// <param name="candidates">Candidate interfaces on the target router.</param>
    /// <param name="interface">Interface whose neighbour is being resolved.</param>
    /// <param name="neighbourReference">Parsed neighbour path used for diagnostics.</param>
    /// <param name="resolvedCandidate">Resolved interface, or <c>null</c> if ambiguity is detected.</param>
    /// <returns>
    /// <c>true</c> if this method reached a final decision; otherwise <c>false</c> when no resolved candidate exists.
    /// </returns>
    private bool TryGetResolvedCandidate(
        Interface[] candidates,
        Interface @interface,
        NeighbourReference neighbourReference,
        out Interface? resolvedCandidate)
    {
        var resolvedCandidates = candidates
            .Where(FilterResolvedNeighbours)
            .ToArray();

        switch (resolvedCandidates.Length)
        {
            case 1:
                resolvedCandidate = resolvedCandidates[0];
                resolvedCandidate.Neighbour = @interface;
                return true;
            case > 1:
                this.Log(@interface,
                    $"Neighbour path '{neighbourReference}' matches multiple interfaces; " +
                    $"specify the neighbour interface explicitly");
                resolvedCandidate = null;
                return true;
            default:
                resolvedCandidate = null;
                return false;
        }

        // Only keep unresolved neighbours
        bool FilterResolvedNeighbours(Interface i)
        {
            return i.Neighbour is not null && (
                (i.Neighbour.Neighbour is null && ReferencePointsToInterface(i, @interface))
                || (i.Neighbour.Neighbour is not null && i.Neighbour == @interface));
        }

        // Determine whether an unresolved neighbour declaration points to the current interface.
        [Pure]
        static bool ReferencePointsToInterface(Interface candidate, Interface current)
        {
            var neighbourReference = new NeighbourReference(candidate.Neighbour!.Name);
            return neighbourReference.AsNumber == current.ParentRouter.ParentAs.Number
                   && neighbourReference.RouterName == current.ParentRouter.Name
                   && (string.IsNullOrEmpty(neighbourReference.InterfaceName)
                       || neighbourReference.InterfaceName == current.Name);
        }
    }

    /// <summary>
    /// Try to resolve a neighbour by guessing among unresolved candidate interfaces.
    /// </summary>
    /// <param name="candidates">Candidate interfaces on the target router.</param>
    /// <param name="interface">Interface whose neighbour is being resolved.</param>
    /// <param name="neighbourReference">Parsed neighbour path used for diagnostics.</param>
    /// <param name="guessedCandidate">Resolved interface, or <c>null</c> if guessing is ambiguous.</param>
    /// <returns>
    /// <c>true</c> if this method reached a final decision; otherwise <c>false</c> when guessing cannot resolve anything.
    /// </returns>
    private bool TryGetGuessedCandidate(
        Interface[] candidates,
        Interface @interface,
        NeighbourReference neighbourReference,
        out Interface? guessedCandidate)
    {
        var guessedCandidates = candidates
            .Where(i => i.Neighbour is null)
            .ToArray();

        switch (guessedCandidates.Length)
        {
            case 1:
                guessedCandidate = guessedCandidates[0];
                this.Log(@interface,
                    $"Neighbour resolved by guessing ({guessedCandidate.ParentRouter.Name}:{guessedCandidate.Name})",
                    logLevel: LogLevel.Warning);

                guessedCandidate.Neighbour = @interface;
                return true;
            case > 1:
                this.Log(@interface,
                    $"Neighbour resolution by guessing for path '{neighbourReference}' matches multiple " +
                    $"interfaces; specify the neighbour interface explicitly");
                guessedCandidate = null;
                return true;
            default:
                guessedCandidate = null;
                return false;
        }
    }
}