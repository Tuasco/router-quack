using System.Data;
using Microsoft.Extensions.Logging;
using RouterQuack.Core.Models;

namespace RouterQuack.Core.Steps;

/// <summary>
/// Run various checks.
/// </summary>
public class Step2RunChecks(ILogger<Step2RunChecks> logger) : IStep
{
    public void Execute(ICollection<As> asses)
    {
        NoDuplicateRouterNames(asses);
        NoDuplicateIpAddress(asses);
        ValidBgpRelationships(asses);
        NoExternalWithoutAddress(asses);
    }

    /// <summary>
    /// Generate an error if there are duplicate router names.
    /// </summary>
    /// <param name="asses"></param>
    private static void NoDuplicateRouterNames(ICollection<As> asses)
    {
        if (asses
            .SelectMany(a => a.Routers)
            .CountBy(n => n.Name)
            .Any(c => c.Value > 1))
            throw new DuplicateNameException("Duplicate routers");
    }

    /// <summary>
    /// Generate an error if there are duplicate IP Addresses.
    /// </summary>
    /// <param name="asses"></param>
    private static void NoDuplicateIpAddress(ICollection<As> asses)
    {
        if (asses
            .SelectMany(a => a.Routers)
            .SelectMany(r => r.Interfaces)
            .SelectMany(i => i.Addresses ?? [])
            .CountBy(a => a.IpAddress)
            .Any(c => c.Value > 1))
            throw new DuplicateNameException("Duplicate IP Addresses");
    }

    /// <summary>
    /// Generate an error if there are uncoherent BGP relationships.
    /// </summary>
    /// <param name="asses"></param>
    /// <remarks>Will generate a warning if there are interfaces with an inter-AS neighbour but no BGP.</remarks>
    private void ValidBgpRelationships(ICollection<As> asses)
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
                throw new InvalidConstraintException("Bgp relationships are not valid");

            // Check if BGP is on if our neighbour is in a different AS
            if (@interface.Bgp == BgpRelationship.None
                && @interface.ParentRouter.ParentAs.Number != @interface.Neighbour.ParentRouter.ParentAs.Number)
                logger.LogWarning(
                    "Interface {InterfaceName} of router {RouterName} of AS number {AsNumber} " +
                    "has a neighbour in another interface, yet doesn't use BGP",
                    @interface.Name,
                    @interface.ParentRouter.Name,
                    @interface.ParentRouter.ParentAs.Number);

            interfaces.Remove(@interface);
            interfaces.Remove(@interface.Neighbour);
        }
    }

    /// <summary>
    /// Generate an error if there are external routers with no manual IP Addresses.
    /// </summary>
    /// <param name="asses"></param>
    private void NoExternalWithoutAddress(ICollection<As> asses)
    {
        var match = asses
            .SelectMany(a => a.Routers)
            .SelectMany(r => r.Interfaces)
            .Any(i => i.ParentRouter.External && i.Addresses is null);

        if (match)
            throw new ArgumentException("No addresses found on external interface");
    }
}