#pragma warning disable CA2254
namespace RouterQuack.IO.Yaml.Parser;

public partial class YamlParser
{
    private void YamlAsToCoreAs(IDictionary<int, YamlAs> asDict, ICollection<As> asses)
    {
        foreach (var (key, value) in asDict)
        {
            IgpType igp;
            try
            {
                igp = asUtils.ParseIgp(value.Igp);
            }
            catch (ArgumentException e)
            {
                this.LogError("AS number {AsNumber}: " + e.Message, key);
                igp = 0;
            }

            IpVersion version;
            try
            {
                version = networkUtils.ParseIpVersion(value.Networks);
            }
            catch (Exception e)
            {
                this.LogError("AS number {AsNumber}: " + e.Message, key);
                version = 0;
            }

            var @as = new As
            {
                Number = key,
                Igp = igp,
                LoopbackSpace = value.LoopbackSpace,
                NetworksSpaceV4 = value.NetworksSpaceV4,
                NetworksSpaceV6 = value.NetworksSpaceV6,
                NetworksIpVersion = version,
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
                this.LogError("AS number {AsNumber}: " + e.Message, key);
                brand = 0;
            }

            @as.Routers = YamlRouterToCoreRouter(value.Routers, @as, brand, value.External);
            asses.Add(@as);
        }
    }
}