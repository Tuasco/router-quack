using RouterQuack.Core.Extensions;
using RouterQuack.Core.Models;
using YamlAs = RouterQuack.IO.Yaml.Models.As;

namespace RouterQuack.IO.Yaml.Parser;

public partial class YamlParser
{
    private ICollection<As> YamlAsToCoreAs(IDictionary<int, YamlAs> asDict)
    {
        ICollection<As> asses = [];

        foreach (var (key, value) in asDict)
        {
            IgpType igp;
            try
            {
                igp = networkUtils.ParseIgp(value.Igp);
            }
            catch (ArgumentException e)
            {
                this.LogError("{ErrorMessage} of AS number {AsNumber}", e.Message, key);
                igp = 0;
            }

            var @as = new As
            {
                Number = key,
                Igp = igp,
                LoopbackSpace = value.LoopbackSpace,
                NetworksSpace = value.NetworksSpace,
                Routers = []
            };

            // Take default router brand
            RouterBrand brand;
            try
            {
                brand = routerUtils.ParseBrand(value.Brand);
            }
            catch (ArgumentException e)
            {
                this.LogError("{ErrorMessage} (default brand) of AS number {AsNumber}", e.Message, key);
                brand = 0;
            }

            @as.Routers = YamlRouterToCoreRouter(value.Routers, @as, brand, value.External);
            asses.Add(@as);
        }

        return asses;
    }
}