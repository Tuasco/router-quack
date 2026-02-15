using RouterQuack.Core.Extensions;
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
                this.LogError("{ErrorMessage} in router {RouterName} of AS number {AsNumber}",
                    e.Message,
                    key,
                    parentAs.Number);
                routerBrand = routerUtils.ParseBrand(defaultBrand.ToString());
            }

            Address? loopbackAddress;
            try
            {
                loopbackAddress = value.Loopback is not null
                    ? networkUtils.ParseIpAddress(value.Loopback)
                    : null;
            }
            catch (ArgumentException)
            {
                this.LogError("Could not parse loopback address {LoopbackAddress} of router {RouterName}",
                    value.Loopback,
                    key);
                loopbackAddress = null;
            }

            var router = new Router
            {
                Name = key,
                Id = value.Id ?? routerUtils.GetDefaultId(key),
                Brand = routerBrand,
                LoopbackAddress = loopbackAddress,
                OspfArea = value.OspfArea,
                Interfaces = [],
                ParentAs = parentAs,
                External = value.External ?? externalAs
            };

            router.Interfaces = YamlInterfaceToCoreInterface(value.Interfaces, router);
            routers.Add(router);
        }

        return routers;
    }
}