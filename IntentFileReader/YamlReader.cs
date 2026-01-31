using System.Data;
using RouterQuack.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlAs = RouterQuack.Models.Yaml.As;
using YamlRouter = RouterQuack.Models.Yaml.Router;
using YamlInterface = RouterQuack.Models.Yaml.Interface;
using static System.Console;

namespace RouterQuack.IntentFileReader;

public class YamlReader : IIntentFileReader
{
    // Throws DuplicateNameException when AS defined multiple times
    // Throws InvalidDataException when interface points to undefined neighbour
    public ICollection<As> ReadFiles(string[] paths)
    {
        var asDict = new Dictionary<int, YamlAs>();
        
        foreach (var path in paths)
        {
            if (!File.Exists(path))
            {
                WriteLine($"File {path} does not exist, skipping");
                continue;
            }
            
            WriteLine($"Reading file {path}");
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance) 
                .Build();

            foreach (var @as in deserializer.Deserialize<IDictionary<int, YamlAs>>(File.ReadAllText(path)))
            {
                if (asDict.ContainsKey(@as.Key))
                    throw new DuplicateNameException($"Duplicate AS number {@as.Key}");
                
                asDict.Add(@as.Key, @as.Value);
            }
        }
        
        WriteLine($"Found {asDict.Count} ASs");
        var asCollection = YamlAsToAs(asDict);
        WriteLine("Intent files parsed successfully");
        return asCollection;
    }

    private ICollection<As> YamlAsToAs(IDictionary<int, YamlAs> asDict)
    {
        ICollection<As> asses = [];

        foreach (var yamlAs in asDict)
        {
            var @as = new As
            {
                Number = yamlAs.Key,
                Igp = Enum.TryParse<IgpType>(yamlAs.Value.Igp, out var igpType) ? igpType : IgpType.Ibgp,
                LoopbackSpace = yamlAs.Value.LoopbackSpace,
                NetworksSpace = yamlAs.Value.NetworksSpace,
                Routers = []
            };

            @as.Routers = YamlRouterToRouter(yamlAs.Value.Routers, @as);
            asses.Add(@as);
        }
        
        foreach (var @as in asses)
            foreach (var router in @as.Routers)
                foreach (var @interface in router.Interfaces)
                    @interface.PopulateNeighbour(asses);
        
        return asses;
    }

    private ICollection<Router> YamlRouterToRouter(IDictionary<string, YamlRouter> routerDict, As parentAs)
    {
        ICollection<Router> routers = [];

        foreach (var yamlRouter in routerDict)
        {
            var router = new Router
            {
                Name = yamlRouter.Key,
                OspfArea =  yamlRouter.Value.OspfArea,
                Interfaces = [],
                ParentAs = parentAs
            };

            router.Interfaces = YamlInterfaceToInterface(yamlRouter.Value.Interfaces, router);
            routers.Add(router);
        }
        

        return routers;
    }

    private ICollection<Interface> YamlInterfaceToInterface(IDictionary<string, YamlInterface> interfaceDict, Router parentRouter)
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
                Bgp = Enum.TryParse<BgpRelationship>(yamlInterface.Value.Bgp, out var bgpRelationship) 
                    ? bgpRelationship 
                    : BgpRelationship.None,
                ParentRouter = parentRouter
            });
        }

        return interfaces;
    }
}