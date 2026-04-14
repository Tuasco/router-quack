namespace RouterQuack.IO.Yaml.Parser;

/// <summary>
/// Maps <see cref="YamlAs"/> definitions to core <see cref="As"/> models.
/// </summary>
public sealed class YamlAsMapper(YamlRouterMapper yamlRouterMapper)
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
            var @as = new As
            {
                Number = key,
                Igp = value.Igp,
                Core = value.Core,
                LoopbackSpaceV4 = value.LoopbackSpaceV4,
                LoopbackSpaceV6 = value.LoopbackSpaceV6,
                NetworksSpaceV4 = value.NetworksSpaceV4,
                NetworksSpaceV6 = value.NetworksSpaceV6,
                AddressFamily = value.AddressFamily,
                Deploy = value.Deploy,
                Routers = []
            };

            @as.Routers = yamlRouterMapper.Map(value.Routers, @as, value.Brand, value.External, context);
            context.Asses.Add(@as);
        }
    }
}