#pragma warning disable CA2254
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
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            // When only the key is declared (no YAML body), value will be null.
            if (value is null)
            {
                this.LogError("Router {RouterName} in AS number {AsNumber}: Body cannot be empty.",
                    key,
                    parentAs.Number);
                continue;
            }

            RouterBrand routerBrand;
            try
            {
                routerBrand = routerUtils.ParseBrand(value.Brand, defaultBrand);
            }
            catch (ArgumentException e)
            {
                this.LogError("Router {RouterName} in AS number {AsNumber}: " + e.Message + '.', key, parentAs.Number);
                routerBrand = routerUtils.ParseBrand(defaultBrand.ToString());
            }

            Address? loopbackAddress;
            try
            {
                loopbackAddress = value.Loopback is not null
                    ? new Address(value.Loopback)
                    : null;
            }
            catch (ArgumentException)
            {
                this.LogError("Router {RouterName} in AS number {AsNumber}: Could not parse loopback address.",
                    key, parentAs.Number);
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