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
            var vrfIndexByName = vrfNames
                .Select((name, i) => (name, index: i + 1))
                .ToDictionary(x => x.name, x => x.index);

            // One counter per CE ASN, each starting at 1
            var rdCounterByCeAsn = new Dictionary<long, int>();

            foreach (var router in @as.Routers)
            {
                foreach (var vrf in router.Vrfs)
                {
                    var ceAsNumber = router.Interfaces
                                         .FirstOrDefault(i => i.Vrf == vrf.Name && i.Neighbour != null)
                                         ?.Neighbour?.ParentRouter.ParentAs.Number
                                     ?? @as.Number;

                    var rtIndex = vrfIndexByName[vrf.Name];

                    if (string.IsNullOrEmpty(vrf.RouteDistinguisher))
                    {
                        if (!rdCounterByCeAsn.TryGetValue(ceAsNumber, out var counter))
                            counter = 0;
                        rdCounterByCeAsn[ceAsNumber] = ++counter;

                        vrf.RouteDistinguisher = $"{ceAsNumber}:{counter}";
                    }

                    if (vrf.ImportTargets is null or { Count: 0 })
                        vrf.ImportTargets = [$"{ceAsNumber}:{rtIndex * 100}"];

                    if (vrf.ExportTargets is null or { Count: 0 })
                        vrf.ExportTargets = [$"{ceAsNumber}:{rtIndex * 100}"];
                }
            }
        }
    }
}