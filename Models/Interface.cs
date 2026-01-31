namespace RouterQuack.Models;

public class Interface
{
    public required string Name { get; set; }

    public required Interface? Neighbour { get; set; }

    public BgpRelationship Bgp { get; set; } = BgpRelationship.None;
    
    public required Router ParentRouter { get; set; }
    

    public Interface PopulateNeighbour(ICollection<As> asses)
    {
        string[] neighbourPath = Neighbour?.Name.Split(':') ?? [];
        int asNumber;
        string routerName = neighbourPath.Last();
        
        if (neighbourPath.Length > 2)
            throw new InvalidDataException("Invalid neighbour format in interface");
        
        // Try and get AS number. If not in neighbourPath, default to ours
        if (neighbourPath.Length == 2)
            asNumber = int.TryParse(neighbourPath.First(), out int index)
                ? index
                : throw new IndexOutOfRangeException("Invalid AS number in interface");
        else
            asNumber = ParentRouter.ParentAs.Number;

        Neighbour = GetNeighbour(asNumber, routerName, asses)
                    ?? throw new InvalidDataException("Invalid neighbour in interface");
        
        return this;
    }

    private Interface? GetNeighbour(int asNumber, string routerName, ICollection<As> asses)
    {
        Console.WriteLine($"Finding neighbour of {Name}");
        
        // Filter in neighbours that don't have neighbours (dummy neighbour with our actual neighbour's name)
        // Or neighbours that do have neighbours, that happen to be ourselves
        bool FilterNeighbours(Interface i) => 
            (i.Neighbour!.Neighbour is null && i.Neighbour.Name.Split(':').Last() == ParentRouter.Name)
            || (i.Neighbour.Neighbour is not null && i.Neighbour == this);
        
        // Try and get our neighbour
        return asses.FirstOrDefault(a => a.Number == asNumber)
            ?.Routers.FirstOrDefault(r => r.Name == routerName)
            ?.Interfaces.FirstOrDefault(FilterNeighbours);
    }
}

public enum BgpRelationship
{
    None,
    Client,
    Peer,
    Provider
}