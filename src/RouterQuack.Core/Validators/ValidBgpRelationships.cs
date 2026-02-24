using RouterQuack.Core.Extensions;

namespace RouterQuack.Core.Validators;

/// <summary>
/// Generate an error if there are uncoherent BGP relationships.
/// </summary>
/// /// <remarks>Will generate a warning if there are interfaces with an inter-AS neighbour but no BGP.</remarks>
public class ValidBgpRelationships(ILogger<ValidBgpRelationships> logger, Context context) : IValidator
{
    public bool ErrorsOccurred { get; set; }
    public string BeginMessage => "Ensuring consistent BGP relationships between neighbours";
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
                this.Log(link.Item1, "Invalid BGP relationship with neighbour");

            // Check if BGP is off when our neighbour is in a different AS
            if (link is not { Item1.Bgp: BgpRelationship.None, Item2.Bgp: BgpRelationship.None }
                && link.Item1.ParentRouter.ParentAs == link.Item2.ParentRouter.ParentAs)
                this.Log(link.Item1, "Neighbour is in the same AS, yet a BGP relationship is defined",
                    logLevel: LogLevel.Warning);

            // Check if BGP is on when our neighbour is in the same AS (impossible if the previous case matched)
            else if (link is { Item1.Bgp: BgpRelationship.None, Item2.Bgp: BgpRelationship.None }
                     && link.Item1.ParentRouter.ParentAs != link.Item2.ParentRouter.ParentAs)
                this.Log(link.Item1, "Neighbour is in a different AS, yet no BGP relationship is defined",
                    logLevel: LogLevel.Warning);
        }
    }
}