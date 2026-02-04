using System.Data;
using RouterQuack.Core.Models;

namespace RouterQuack.Core.Steps;

public class Step2RunChecks : IStep
{
    public void Execute(ICollection<As> asses)
    {
        NoDuplicateRouterNames(asses);
        NoDuplicateIpAddress(asses);
    }

    private static void NoDuplicateRouterNames(ICollection<As> asses)
    {
        if (asses
            .SelectMany(a => a.Routers)
            .CountBy(n => n.Name)
            .Any(c => c.Value > 1))
            throw new DuplicateNameException("Duplicate routers");
    }

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
}