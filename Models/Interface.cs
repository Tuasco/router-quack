namespace RouterQuack.Models;

public class Interface
{
    public required string Name { get; set; }

    public required Interface? Neighbour { get; set; }

    public BgpRelationship Bgp { get; set; } = BgpRelationship.None;
    
    public required Router ParentRouter { get; set; }
    

    public Interface PopulateNeighbour(ICollection<As> asses)
    {
        var neighbourPath = Neighbour?.Name.Split(':') ?? [];
        var routerName = neighbourPath.Last();

        string errorEnd =
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
}

public enum BgpRelationship
{
    None,
    Client,
    Peer,
    Provider
}