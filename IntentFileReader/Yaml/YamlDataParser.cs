using RouterQuack.Models;
using YamlAs = RouterQuack.Models.Yaml.As;
using YamlRouter = RouterQuack.Models.Yaml.Router;
using YamlInterface = RouterQuack.Models.Yaml.Interface;

namespace RouterQuack.IntentFileReader.Yaml;

public partial class YamlReader
{
    private static ICollection<As> YamlAsToAs(IDictionary<int, YamlAs> asDict)
    {
        ICollection<As> asses = [];

        foreach (var yamlAs in asDict)
        {
            var @as = new As
            {
                Number = yamlAs.Key,
                Igp = Enum.TryParse<IgpType>(yamlAs.Value.Igp, ignoreCase: true, out var igpType) ? igpType : IgpType.Ibgp,
                LoopbackSpace = yamlAs.Value.LoopbackSpace,
                NetworksSpace = yamlAs.Value.NetworksSpace,
                Routers = []
            };
            
            // Take default router brand
            var brand = Enum.TryParse<RouterBrand>(yamlAs.Value.Brand, ignoreCase: true, out var routerBrand)
                ? routerBrand
                : RouterBrand.Unknwon;
            
            @as.Routers = YamlRouterToRouter(yamlAs.Value.Routers, @as, brand);
            asses.Add(@as);
        }
        
        foreach (var @as in asses)
            foreach (var router in @as.Routers)
                foreach (var @interface in router.Interfaces)
                    @interface.PopulateNeighbour(asses);
        
        return asses;
    }

    private static ICollection<Router> YamlRouterToRouter(IDictionary<string, YamlRouter> routerDict, As parentAs, RouterBrand defaultBrand)
    {
        ICollection<Router> routers = [];

        foreach (var yamlRouter in routerDict)
        {
            var router = new Router
            {
                Name = yamlRouter.Key,
                Id = yamlRouter.Value.Id ?? Router.GetDefaultId(yamlRouter.Key),
                OspfArea =  yamlRouter.Value.OspfArea,
                Interfaces = [],
                ParentAs = parentAs,
                Brand = Enum.TryParse<RouterBrand>(yamlRouter.Value.Brand, ignoreCase: true, out var routerBrand) 
                    ? routerBrand
                    : defaultBrand
            };
            
            router.Interfaces = YamlInterfaceToInterface(yamlRouter.Value.Interfaces, router);
            routers.Add(router);
        }
        

        return routers;
    }

    private static ICollection<Interface> YamlInterfaceToInterface(IDictionary<string, YamlInterface> interfaceDict, Router parentRouter)
    {
        ICollection<Interface> interfaces = [];

        foreach (var yamlInterface in interfaceDict)
        {
            var dummyNeighbour = new Interface
            {
                Name = yamlInterface.Value.Neighbour,
                Neighbour = null,
                ParentRouter = parentRouter
            };

            interfaces.Add(new Interface
            {
                Name = yamlInterface.Key,
                Neighbour = dummyNeighbour, // Populate it now, will resolve it later in YamlAsToAs
                Bgp = Enum.TryParse<BgpRelationship>(yamlInterface.Value.Bgp, ignoreCase: true, out var bgpRelationship)
                    ? bgpRelationship
                    : BgpRelationship.None,
                ParentRouter = parentRouter
            });
        }

        return interfaces;
    }
}