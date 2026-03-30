using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace RouterQuack.IO.Yaml.Parser;

/// <summary>
/// Maps <see cref="YamlRouter"/> definitions to core <see cref="Router"/> models.
/// </summary>
public class YamlRouterMapper(ILogger<YamlRouterMapper> logger, YamlInterfaceMapper yamlInterfaceMapper)
{
    /// <summary>
    /// Convert <see cref="YamlRouter"/> definitions of an AS into core router models.
    /// </summary>
    /// <param name="routerDict"><see cref="YamlRouter"/> definitions keyed by router name.</param>
    /// <param name="parentAs"><see cref="As"/> definitions that owns the mapped routers.</param>
    /// <param name="defaultBrand">Default brand inherited from the parent <see cref="As"/>.</param>
    /// <param name="externalAs">Whether routers should default to external.</param>
    /// <param name="context">Using execution context.</param>
    /// <returns>The mapped routers.</returns>
    public ICollection<Router> Map(IDictionary<string, YamlRouter> routerDict,
        As parentAs,
        RouterBrand defaultBrand,
        bool externalAs,
        Context context)
    {
        ICollection<Router> routers = [];

        foreach (var (key, value) in routerDict)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            // When only the key is declared (no YAML body), value will be null.
            if (value is null)
            {
                LogError(context, "Router {RouterName} in AS number {AsNumber}: Body cannot be empty.",
                    key, parentAs.Number);
                continue;
            }

            var router = new Router
            {
                Name = key,
                Id = value.Id,
                Brand = value.Brand ?? defaultBrand,
                LoopbackAddressV4 = value.LoopbackV4,
                LoopbackAddressV6 = value.LoopbackV6,
                Bgp = value.Bgp,
                AdditionalConfig = value.AdditionalConfig,
                Interfaces = [],
                ParentAs = parentAs,
                External = value.External ?? externalAs
            };

            router.Interfaces = yamlInterfaceMapper.Map(value.Interfaces, router, context);
            routers.Add(router);
        }

        return routers;
    }

    private void LogError(Context context, [StructuredMessageTemplate] string message, params object?[] args)
    {
        logger.LogError(message, args);
        context.ApplyError();
    }
}