namespace RouterQuack.Core.Processors;

/// <summary>
/// Auto-generates RD and RT for VRFs that don't have them explicitly configured.
/// RD format: {ASN}:{vrfIndex}, RT format: {ASN}:{vrfIndex * 100}
/// </summary>
public class PopulateVrfRdRt(ILogger<PopulateVrfRdRt> logger, Context context) : IProcessor
{
    public string BeginMessage => "Populating VRF route distinguishers and route targets";
    public ILogger Logger { get; } = logger;
    public Context Context { get; } = context;

    public void Process()
    {
        foreach (var @as in Context.Asses)
        {
            // Collect all unique VRF names across all routers in this AS
            var vrfNames = @as.Routers
                .SelectMany(r => r.Vrfs)
                .Select(v => v.Name)
                .Distinct()
                .OrderBy(n => n) // deterministic ordering
                .ToList();
            Console.WriteLine($"Router count: {@as.Routers.Count}");
            foreach (var router in @as.Routers)
            {
                Console.WriteLine($"Router name: {router.Name} has {router.Vrfs.Count} vrfs");
                foreach (var vrf in router.Vrfs)
                {
                    var vrfIndex = vrfNames.IndexOf(vrf.Name) + 1; // 1-based

                    vrf.RouteDistinguisher ??= $"{@as.Number}:{vrfIndex}";
                    Console.WriteLine($"{vrf.Name}: {vrf.RouteDistinguisher} writing !");
                    if (vrf.ImportTargets == null || !vrf.ImportTargets.Any())
                        vrf.ImportTargets = [$"{@as.Number}:{vrfIndex * 100}"];

                    if (vrf.ExportTargets == null || !vrf.ExportTargets.Any())
                        vrf.ExportTargets = [$"{@as.Number}:{vrfIndex * 100}"];
                }
            }
        }
    }
}