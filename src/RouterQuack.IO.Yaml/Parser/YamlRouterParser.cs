using Microsoft.Extensions.Logging;
using RouterQuack.Core.Models;
using YamlRouter = RouterQuack.IO.Yaml.Models.Router;

namespace RouterQuack.IO.Yaml.Parser;

public partial class YamlParser
{
    private ICollection<Router> YamlRouterToCoreRouter(IDictionary<string, YamlRouter> routerDict,
        As parentAs,
        RouterBrand defaultBrand,
        bool externalAs)
    {
        ICollection<Router> routers = [];

        foreach (var (key, value) in routerDict)
        {
            RouterBrand routerBrand;
            try
            {
                routerBrand = routerUtils.ParseBrand(value.Brand, defaultBrand);
            }
            catch (ArgumentException e)
            {
                logger.LogError("{ErrorMessage} in router {RouterName} of AS number {AsNumber}",
                    e.Message,
                    key,
                    parentAs.Number);
                ErrorsOccurred = true;
                routerBrand = routerUtils.ParseBrand(defaultBrand.ToString());
            }

            var router = new Router
            {
                Name = key,
                Id = value.Id ?? routerUtils.GetDefaultId(key),
                OspfArea = value.OspfArea,
                Interfaces = [],
                ParentAs = parentAs,
                Brand = routerBrand,
                External = value.External ?? externalAs
            };

            router.Interfaces = YamlInterfaceToCoreInterface(value.Interfaces, router);
            routers.Add(router);
        }

        return routers;
    }
}