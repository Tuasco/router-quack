using RouterQuack.Core.Models;
using YamlAs = RouterQuack.IO.Yaml.Models.As;
using YamlRouter = RouterQuack.IO.Yaml.Models.Router;
using YamlInterface = RouterQuack.IO.Yaml.Models.Interface;

namespace RouterQuack.IO.Yaml;

public partial class YamlReader
{
    private ICollection<As> YamlAsToCoreAs(IDictionary<int, YamlAs> asDict)
    {
        ICollection<As> asses = [];

        foreach (var (key, value) in asDict)
        {
            var @as = new As
            {
                Number = key,
                Igp = networkUtils.ParseIgp(value.Igp),
                LoopbackSpace = value.LoopbackSpace,
                NetworksSpace = value.NetworksSpace,
                Routers = []
            };
            
            // Take default router brand
            var brand = routerUtils.ParseBrand(value.Brand);
            
            @as.Routers = YamlRouterToCoreRouter(value.Routers, @as, brand, value.External);
            asses.Add(@as);
        }
        
        return asses;
    }

    private ICollection<Router> YamlRouterToCoreRouter(IDictionary<string, YamlRouter> routerDict,
        As parentAs,
        RouterBrand defaultBrand,
        bool externalAs)
    {
        ICollection<Router> routers = [];

        foreach (var (key, value) in routerDict)
        {
            var router = new Router
            {
                Name = key,
                Id = value.Id ?? routerUtils.GetDefaultId(key),
                OspfArea =  value.OspfArea,
                Interfaces = [],
                ParentAs = parentAs,
                Brand = routerUtils.ParseBrand(value.Brand, defaultBrand),
                External = value.External ?? externalAs
            };
            
            router.Interfaces = YamlInterfaceToCoreInterface(value.Interfaces, router);
            routers.Add(router);
        }

        return routers;
    }

    private ICollection<Interface> YamlInterfaceToCoreInterface(IDictionary<string, YamlInterface> interfaceDict,
        Router parentRouter)
    {
        ICollection<Interface> interfaces = [];

        foreach (var (key, value) in interfaceDict)
        {
            var dummyNeighbour = new Interface
            {
                Name = value.Neighbour,
                Neighbour = null,
                ParentRouter = parentRouter
            };

            interfaces.Add(new Interface
            {
                Name = key,
                Neighbour = dummyNeighbour, // Populate it now, will resolve it later in YamlAsToAs
                ParentRouter = parentRouter,
                Addresses = value.Addresses?.Select(networkUtils.ParseIpAddress).ToList(),
                Bgp = networkUtils.ParseBgp(value.Bgp)
            });
        }

        return interfaces;
    }
}