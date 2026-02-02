using RouterQuack.Models;
using RouterQuack.Utils;
using YamlAs = RouterQuack.Models.Yaml.As;
using YamlRouter = RouterQuack.Models.Yaml.Router;
using YamlInterface = RouterQuack.Models.Yaml.Interface;

namespace RouterQuack.IntentFileReader.Yaml;

public partial class YamlReader(INetworkUtils networkUtils)
{
    private ICollection<As> YamlAsToAs(IDictionary<int, YamlAs> asDict)
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
            var brand = networkUtils.ParseBrand(value.Brand);
            
            @as.Routers = YamlRouterToRouter(value.Routers, @as, brand);
            asses.Add(@as);
        }
        
        return asses;
    }

    private ICollection<Router> YamlRouterToRouter(IDictionary<string, YamlRouter> routerDict, As parentAs, RouterBrand defaultBrand)
    {
        ICollection<Router> routers = [];

        foreach (var (key, value) in routerDict)
        {
            var router = new Router
            {
                Name = key,
                Id = value.Id ?? networkUtils.GetDefaultId(key),
                OspfArea =  value.OspfArea,
                Interfaces = [],
                ParentAs = parentAs,
                Brand = networkUtils.ParseBrand(value.Brand, defaultBrand)
            };
            
            router.Interfaces = YamlInterfaceToInterface(value.Interfaces, router);
            routers.Add(router);
        }

        return routers;
    }

    private ICollection<Interface> YamlInterfaceToInterface(IDictionary<string, YamlInterface> interfaceDict, Router parentRouter)
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