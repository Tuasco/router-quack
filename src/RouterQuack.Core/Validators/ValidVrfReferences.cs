using RouterQuack.Core.Extensions;

namespace RouterQuack.Core.Validators;

/// <summary>
/// Generate an error if an interface references a VRF that is not declared on its router.
/// </summary>
public class ValidVrfReferences(ILogger<ValidVrfReferences> logger, Context context) : IValidator
{
    public string BeginMessage => "Ensuring all interface VRF references are declared on their router";
    public ILogger Logger { get; } = logger;
    public Context Context { get; } = context;

    public void Validate()
    {
        foreach (var router in Context.Asses.SelectMany(a => a.Routers))
        {
            var declaredVrfs = router.Vrfs.Select(v => v.Name).ToHashSet();

            foreach (var iface in router.Interfaces)
            {
                if (iface.Vrf is not null && !declaredVrfs.Contains(iface.Vrf))
                    this.LogError(
                        "Interface \"{InterfaceName}\" on router \"{RouterName}\" references undeclared VRF \"{VrfName}\".",
                        iface.Name, router.Name, iface.Vrf);
            }
        }
    }
}