using RouterQuack.Core.Models;

namespace RouterQuack.Core.Steps;

public class Step1ResolveNeighbours : IStep
{
    public void Execute(ICollection<As> asses)
    {
        foreach (var @as in asses)
            foreach (var router in @as.Routers)
                foreach (var @interface in router.Interfaces)
                    ResolveNeighbour(asses, @interface);
    }
    
    private void ResolveNeighbour(ICollection<As> asses, Interface @interface)
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

        @interface.Neighbour = GetNeighbour(asses, @interface, asNumber, routerName)
                              ?? throw new InvalidDataException("Invalid neighbour" + errorEnd);
    }

    private Interface? GetNeighbour(ICollection<As> asses, Interface @interface, int asNumber, string routerName)
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