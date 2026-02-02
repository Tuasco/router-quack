using System.Net;

namespace RouterQuack.Models;

public class Interface
{
    public required string Name { get; init; }

    // Initially, this will point to a dummy interface, with one relevant piece of information
    // which is Name. It will be used to resolve the actual neighbour interface
    // Starting from the collection of ASs
    public required Interface? Neighbour { get; set; }

    public BgpRelationship Bgp { get; init; } = BgpRelationship.None;
    
    public ICollection<Address>? Addresses { get; init; }
    
    public required Router ParentRouter { get; init; }
    

    public Interface ResolveNeighbour(ICollection<As> asses)
    {
        var neighbourPath = Neighbour?.Name.Split(':') ?? [];
        var routerName = neighbourPath.Last();

        var errorEnd =
            $" in interface {Name} of router {ParentRouter.Name} (AS number {ParentRouter.ParentAs.Number}).";
        
        var asNumber = neighbourPath.Length switch
        {
            1 => ParentRouter.ParentAs.Number,
            2 =>
                // Try and get AS number. If not in neighbourPath, default to ours
                int.TryParse(neighbourPath.First(), out var index)
                ? index
                : throw new IndexOutOfRangeException("Invalid AS number" + errorEnd),
            _ => throw new InvalidDataException("Invalid neighbour formater" + errorEnd)
        };

        Neighbour = GetNeighbour(asNumber, routerName, asses)
                    ?? throw new InvalidDataException("Invalid neighbour" + errorEnd);
        
        return this;
    }

    private Interface? GetNeighbour(int asNumber, string routerName, ICollection<As> asses)
    {
        // Try and get our neighbour
        return asses.FirstOrDefault(a => a.Number == asNumber)
            ?.Routers.FirstOrDefault(r => r.Name == routerName)
            ?.Interfaces.FirstOrDefault(FilterNeighbours);

        // Filter in neighbours that don't have neighbours (dummy neighbour with our actual neighbour's name)
        // Or neighbours that do have neighbours, that happen to be ourselves
        bool FilterNeighbours(Interface i) => 
            (i.Neighbour!.Neighbour is null && i.Neighbour.Name.Split(':').Last() == ParentRouter.Name)
            || (i.Neighbour.Neighbour is not null && i.Neighbour == this);
    }


    public override string ToString() => $"  - Interface {Name} -> {Neighbour?.ParentRouter.Name}";
}

public class Address(IPNetwork networkAddress, IPAddress ipAddress)
{
    public IPNetwork NetworkAddress { get; } = networkAddress;
    
    public IPAddress IpAddress { get; } = ipAddress;
}

public enum BgpRelationship
{
    None,
    Client,
    Peer,
    Provider
}