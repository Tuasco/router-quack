using RouterQuack.Core.Extensions;
using RouterQuack.Core.Models;
using YamlInterface = RouterQuack.IO.Yaml.Models.Interface;

namespace RouterQuack.IO.Yaml.Parser;

public partial class YamlParser
{
    private ICollection<Interface> YamlInterfaceToCoreInterface(IDictionary<string, YamlInterface> interfaceDict,
        Router parentRouter)
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
                try
                {
                    addresses.Add(networkUtils.ParseIpAddress(address));
                }
                catch (ArgumentException e)
                {
                    this.LogError("{ErrorMessage}: {IpAddress} of interface {InterfaceName} in router {RouterName} " +
                                  "of AS number {AsNumber}",
                        e.Message,
                        address,
                        key,
                        parentRouter.Name,
                        parentRouter.ParentAs.Number);
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
}