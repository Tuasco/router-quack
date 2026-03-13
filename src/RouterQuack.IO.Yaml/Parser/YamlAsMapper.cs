using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using RouterQuack.Core.Utils;
namespace RouterQuack.IO.Yaml.Parser;

/// <summary>
/// Maps <see cref="YamlAs"/> definitions to core <see cref="As"/> models.
/// </summary>
public sealed class YamlAsMapper(
    ILogger<YamlAsMapper> logger,
    AsUtils asUtils,
    NetworkUtils networkUtils,
    RouterUtils routerUtils,
    YamlRouterMapper yamlRouterMapper)
{
    /// <summary>
    /// Convert <see cref="YamlAs"/> definitions into core models and append them to the context.
    /// </summary>
    /// <param name="asDict"><see cref="YamlAs"/> definitions keyed by As number.</param>
    /// <param name="context">Using execution context.</param>
    public void Map(IDictionary<int, YamlAs> asDict, Context context)
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
                LogError(context, "AS number {AsNumber}: {ErrorMessage}", key, e.Message);
                igp = 0;
            }
            IpVersion version;
            try
            {
                version = networkUtils.ParseIpVersion(value.Networks);
            }
            catch (Exception e)
            {
                LogError(context, "AS number {AsNumber}: {ErrorMessage}", key, e.Message);
                version = 0;
            }
            var @as = new As
            {
                Number = key,
                Igp = igp,
                LoopbackSpaceV4 = value.LoopbackSpaceV4,
                LoopbackSpaceV6 = value.LoopbackSpaceV6,
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
                LogError(context, "AS number {AsNumber}: {ErrorMessage}", key, e.Message);
                brand = 0;
            }
            @as.Routers = yamlRouterMapper.Map(value.Routers, @as, brand, value.External, context);
            context.Asses.Add(@as);
        }
    }
    private void LogError(Context context, [StructuredMessageTemplate] string message, params object?[] args)
    {
        logger.LogError(message, args);
        context.ApplyError();
    }
}
