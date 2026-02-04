using RouterQuack.Core.Models;

namespace RouterQuack.Core.Steps;

public class Step1ResolveNeighbours : IStep
{
    public void Execute(ICollection<As> asses)
    {
        foreach (var @as in asses)
            foreach (var router in @as.Routers)
                foreach (var @interface in router.Interfaces)
                    @interface.ResolveNeighbour(asses);
    }
}