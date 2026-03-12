using Microsoft.Extensions.Logging;
using RouterQuack.Core.Utils;
namespace RouterQuack.IO.Yaml.Parser;

/// <summary>
/// Maps <see cref="YamlInterface"/> definitions to core <see cref="Interface"/> models.
/// </summary>
public class YamlInterfaceMapper(
    ILogger<YamlInterfaceMapper> logger,
    InterfaceUtils interfaceUtils)
{
    /// <summary>
    /// Convert <see cref="YamlInterface"/> definitions of a router into core interface models.
    /// </summary>
    /// <param name="interfaceDict"><see cref="YamlInterface"/> definitions keyed by interface name.</param>
    /// <param name="parentRouter">Router that owns the mapped interfaces.</param>
    /// <param name="context">Using execution context.</param>
    /// <returns>The mapped interfaces.</returns>
    public ICollection<Interface> Map(IDictionary<string, YamlInterface> interfaceDict, Router parentRouter, Context context)
    {
        ICollection<Interface> interfaces = [];
        foreach (var (key, value) in interfaceDict)
        {
            var dummyNeighbour = new Interface
            {
                Name = value.Neighbour,
                ParentRouter = parentRouter,
                Neighbour = null,
                Addresses = []
            };
            ICollection<Address> addresses = [];
            foreach (var address in value.Addresses ?? [])
            {
                try
                {
                    addresses.Add(new(address));
                }
                catch (ArgumentException e)
                {
                    LogInterfaceError(context, e.Message, dummyNeighbour);
                }
            }
            interfaces.Add(new()
            {
                Name = key,
                Neighbour = dummyNeighbour, // Populate it now, will resolve it later in Step 1
                ParentRouter = parentRouter,
                Addresses = addresses,
                Bgp = interfaceUtils.ParseBgp(value.Bgp)
            });
        }
        return interfaces;
    }
    private void LogInterfaceError(Context context, string message, Interface @interface)
    {
        logger.LogError(
            "Interface {InterfaceName} of router {RouterName} in AS number {AsNumber}: {Message}.",
            @interface.Name, @interface.ParentRouter.Name, @interface.ParentRouter.ParentAs.Number, message);
        context.ApplyError();
    }
}
