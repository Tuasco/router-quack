using Microsoft.Extensions.Logging;
using RouterQuack.Core.Extensions;
using RouterQuack.Core.Models;

namespace RouterQuack.Core.Validators;

/// <summary>
/// Generate an error if there are uncoherent BGP relationships.
/// </summary>
/// /// <remarks>Will generate a warning if there are interfaces with an inter-AS neighbour but no BGP.</remarks>
public class ValidBgpRelationships(ILogger<ValidBgpRelationships> logger, Context context) : IValidator
{
    public bool ErrorsOccurred { get; set; }
    public string? BeginMessage => null;
    public ILogger Logger { get; } = logger;
    public Context Context { get; } = context;


    public void Validate()
    {
        var links = Context.Asses.GetAllLinks();

        foreach (var link in links)
        {
            // Check if our neighbour's BGP strategy matches ours
            if (link
                is not ({ Item1.Bgp: BgpRelationship.None, Item2.Bgp: BgpRelationship.None }
                or { Item1.Bgp: BgpRelationship.Peer, Item2.Bgp: BgpRelationship.Peer }
                or { Item1.Bgp: BgpRelationship.Client, Item2.Bgp: BgpRelationship.Provider }
                or { Item1.Bgp: BgpRelationship.Provider, Item2.Bgp: BgpRelationship.Client }))
                this.LogError("Invalid BGP relationship between interface {InterfaceName} of router {RouterName} " +
                              "in AS number {AsNumber}.",
                    link.Item1.Name,
                    link.Item1.ParentRouter.Name,
                    @link.Item1.ParentRouter.ParentAs.Number);

            // Check if BGP is off when our neighbour is in the same AS
            if (link is not { Item1.Bgp: BgpRelationship.None, Item2.Bgp: BgpRelationship.None }
                && link.Item1.ParentRouter.ParentAs.Number == link.Item2.ParentRouter.ParentAs.Number)
                this.LogWarning(
                    "Interface {InterfaceName} of router {RouterName} in AS number {AsNumber} " +
                    "and its neighbour are in the same AS, yet define a BGP relationship.",
                    link.Item1.Name,
                    link.Item1.ParentRouter.Name,
                    link.Item1.ParentRouter.ParentAs.Number);

            // Check if BGP is on when our neighbour is in the same AS
            if (link is { Item1.Bgp: BgpRelationship.None, Item2.Bgp: BgpRelationship.None }
                && link.Item1.ParentRouter.ParentAs.Number != link.Item2.ParentRouter.ParentAs.Number)
                this.LogWarning(
                    "Interface {InterfaceName} of router {RouterName} in AS number {AsNumber} " +
                    "has a neighbour in another interface, yet doesn't use BGP.",
                    link.Item1.Name,
                    link.Item1.ParentRouter.Name,
                    link.Item1.ParentRouter.ParentAs.Number);
        }
    }
}