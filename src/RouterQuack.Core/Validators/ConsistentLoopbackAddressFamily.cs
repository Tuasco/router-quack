using RouterQuack.Core.Extensions;

namespace RouterQuack.Core.Validators;

public class ConsistentLoopbackAddressFamily(
    ILogger<ConsistentLoopbackAddressFamily> logger,
    Context context) : IValidator
{
    public bool ErrorsOccurred { get; set; }
    public string BeginMessage => "Ensuring loopback address families are consistent";
    public ILogger Logger { get; } = logger;
    public Context Context { get; } = context;

    public void Validate()
    {
        foreach (var @as in Context.Asses)
        {
            var defaultFamily = @as.LoopbackSpace?.BaseAddress.AddressFamily;
            Console.WriteLine($"Default family : {defaultFamily}");

            foreach (var router in @as.Routers)
            {
                if (defaultFamily == null)
                {
                    defaultFamily = router.LoopbackAddress?.IpAddress.AddressFamily;
                    continue;
                }

                if ((router.LoopbackAddress?.IpAddress.AddressFamily ?? defaultFamily) != defaultFamily)
                    this.Log(router, "Inconsistent loopback address with other routers and/or loopback space");
            }
        }
    }
}