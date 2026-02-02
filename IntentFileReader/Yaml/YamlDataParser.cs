using System.Net;
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

        foreach (var (key, value) in asDict)
        {
            var @as = new As
            {
                Number = key,
                Igp = Enum.TryParse<IgpType>(value.Igp, ignoreCase: true, out var igpType) ? igpType : IgpType.Ibgp,
                LoopbackSpace = value.LoopbackSpace,
                NetworksSpace = value.NetworksSpace,
                Routers = []
            };
            
            // Take default router brand
            var brand = Enum.TryParse<RouterBrand>(value.Brand, ignoreCase: true, out var routerBrand)
                ? routerBrand
                : RouterBrand.Unknwon;
            
            @as.Routers = YamlRouterToRouter(value.Routers, @as, brand);
            asses.Add(@as);
        }
        
        // foreach (var @as in asses)
        //     foreach (var router in @as.Routers)
        //         foreach (var @interface in router.Interfaces)
        //             @interface.PopulateNeighbour(asses);
        
        return asses;
    }

    private static ICollection<Router> YamlRouterToRouter(IDictionary<string, YamlRouter> routerDict, As parentAs, RouterBrand defaultBrand)
    {
        ICollection<Router> routers = [];

        foreach (var (key, value) in routerDict)
        {
            var router = new Router
            {
                Name = key,
                Id = value.Id ?? Router.GetDefaultId(key),
                OspfArea =  value.OspfArea,
                Interfaces = [],
                ParentAs = parentAs,
                Brand = Enum.TryParse<RouterBrand>(value.Brand, ignoreCase: true, out var routerBrand) 
                    ? routerBrand
                    : defaultBrand
            };
            
            router.Interfaces = YamlInterfaceToInterface(value.Interfaces, router);
            routers.Add(router);
        }

        return routers;
    }

    private static ICollection<Interface> YamlInterfaceToInterface(IDictionary<string, YamlInterface> interfaceDict, Router parentRouter)
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
                Addresses = value.Addresses?.Select(TranslateIpAddress).ToList(),
                Bgp = Enum.TryParse<BgpRelationship>(value.Bgp, ignoreCase: true, out var bgpRelationship)
                    ? bgpRelationship
                    : BgpRelationship.None
            });
        }

        return interfaces;
    }

    private static Address TranslateIpAddress(string ip)
    {
        var parts = ip.Split('/');
        
        if(parts.Length != 2)
            throw new InvalidCastException("Couldn't translate IP address");
        
        if (!int.TryParse(parts[1], out var mask))
            throw new InvalidCastException("Couldn't translate IP address (invalid mask)");
            
        if (!IPAddress.TryParse(parts[0], out var ipAddress))
            throw new InvalidCastException("Couldn't translate IP address (invalid IP)");
        
        return new (new (ipAddress, mask), ipAddress);
    }
}