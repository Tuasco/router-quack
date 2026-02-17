using Microsoft.Extensions.Logging;
using RouterQuack.Core.Extensions;
using RouterQuack.Core.Models;

namespace RouterQuack.Core.Validators;

/// <summary>
/// Generate an error if there are uncoherent BGP relationships.
/// </summary>
/// /// <remarks>Will generate a warning if there are interfaces with an inter-AS neighbour but no BGP.</remarks>
public class ValidBgpRelationships(ILogger<ValidBgpRelationships> logger) : IValidator
{
    public bool ErrorsOccurred { get; set; }
    public string? BeginMessage { get; init; } = null;
    public ILogger Logger { get; set; } = logger;

    public void Validate(ICollection<As> asses)
    {
        var interfaces = asses
            .SelectMany(a => a.Routers)
            .SelectMany(r => r.Interfaces)
            .ToList();

        while (interfaces.Count > 0)
        {
            var @interface = interfaces.First();

            // Check if our neighbour's BGP strategy matches ours
            if (@interface
                is not ({ Bgp: BgpRelationship.None, Neighbour.Bgp: BgpRelationship.None }
                or { Bgp: BgpRelationship.Peer, Neighbour.Bgp: BgpRelationship.Peer }
                or { Bgp: BgpRelationship.Client, Neighbour.Bgp: BgpRelationship.Provider }
                or { Bgp: BgpRelationship.Provider, Neighbour.Bgp: BgpRelationship.Client }))
                this.LogError("Invalid BGP relationship between interface {InterfaceName} of router {RouterName} " +
                              "in AS number {AsNumber}",
                    @interface.Name,
                    @interface.ParentRouter.Name,
                    @interface.ParentRouter.ParentAs.Number);

            // Check if BGP is on if our neighbour is in a different AS
            if (@interface.Bgp == BgpRelationship.None
                && @interface.ParentRouter.ParentAs.Number != @interface.Neighbour!.ParentRouter.ParentAs.Number)
                this.LogWarning(
                    "Interface {InterfaceName} of router {RouterName} in AS number {AsNumber} " +
                    "has a neighbour in another interface, yet doesn't use BGP",
                    @interface.Name,
                    @interface.ParentRouter.Name,
                    @interface.ParentRouter.ParentAs.Number);

            interfaces.Remove(@interface);
            interfaces.Remove(@interface.Neighbour!);
        }
    }
}