using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using RouterQuack.Core;
using RouterQuack.Core.Extensions;
using RouterQuack.Core.IntentFileParsers;
using RouterQuack.Core.Models;
using RouterQuack.Core.Utils;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlAs = RouterQuack.IO.Yaml.Models.As;

namespace RouterQuack.IO.Yaml.Parser;

public partial class YamlParser(
    NetworkUtils networkUtils,
    AsUtils asUtils,
    RouterUtils routerUtils,
    InterfaceUtils interfaceUtils,
    ILogger<YamlParser> logger) : IIntentFileParser
{
    [GeneratedRegex(@"\.ya?ml$")]
    private static partial Regex YamlEnding();

    public bool ErrorsOccurred { get; set; }
    public string? BeginMessage { get; init; } = "Parsing intent file(s)";
    public ILogger Logger { get; set; } = logger;

    public void ReadFiles(string[] filePaths, ICollection<As> asses)
    {
        var asDict = new Dictionary<int, YamlAs>();

        foreach (var path in filePaths)
        {
            if (!File.Exists(path))
            {
                logger.LogWarning("File {Path} not found, skipping.", path);
                continue;
            }

            if (!YamlEnding().IsMatch(path))
            {
                logger.LogWarning("File {Path} is not YAML, skipping.", path);
                continue;
            }

            logger.LogDebug("Reading file {Path}", path);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            var fileData = deserializer.Deserialize<IDictionary<int, YamlAs>>(File.ReadAllText(path));
            foreach (var @as in fileData)
                AddAs(@as.Key, @as.Value, asDict);
        }

        logger.LogDebug("Found {AsNumber} ASs", asDict.Count);
        YamlAsToCoreAs(asDict, asses);

        if (ErrorsOccurred)
            throw new StepException();
    }

    /// <summary>
    /// Smartly add <paramref name="@as"/> to <paramref name="asDict"/>.
    /// </summary>
    /// <param name="asNumber">The AS number.</param>
    /// <param name="as">The <see cref="YamlAs"/> to add.</param>
    /// <param name="asDict">Dictionary of <see cref="YamlAs"/> read from YAML intent file.</param>
    /// <remarks>
    /// This method accepts duplicate ASs if they do not have duplicate properties with different values,
    /// or duplicates routers.
    /// </remarks>
    private void AddAs(int asNumber, YamlAs @as, Dictionary<int, YamlAs> asDict)
    {
        if (asDict.TryAdd(asNumber, @as))
            return;

        var currentAs = asDict[asNumber];

        #region ReplaceWithReflection

        /* This function could be more future-proof
         * If we use reflections instead, we could get all the properties that are nullable
         * And do the same check but in a loop
         */

        if (currentAs.Brand is not null
            && @as.Brand is not null
            && @as.Brand != currentAs.Brand)
            this.LogError("AS {AsNumber} defines more than one brand.", asNumber);

        if (currentAs.LoopbackSpace is not null &&
            @as.LoopbackSpace is not null &&
            @as.LoopbackSpace != currentAs.LoopbackSpace)
            this.LogError("AS {AsNumber} defines more than one loopback space.", asNumber);

        if (currentAs.Igp is not null
            && @as.Igp is not null
            && @as.Igp != currentAs.Igp)
            this.LogError("AS {AsNumber} defines more than one IGP.", asNumber);

        if (currentAs.Networks is not null
            && @as.Networks is not null
            && @as.Networks != currentAs.Networks)
            this.LogError("AS {AsNumber} defines more than one network type (networks).", asNumber);

        if (currentAs.NetworksSpaceV4 is not null
            && @as.NetworksSpaceV4 is not null
            && @as.NetworksSpaceV4 != currentAs.NetworksSpaceV4)
            this.LogError("AS {AsNumber} defines more than one IPv4 networks space.", asNumber);

        if (currentAs.NetworksSpaceV6 is not null
            && @as.NetworksSpaceV6 is not null
            && @as.NetworksSpaceV6 != currentAs.NetworksSpaceV6)
            this.LogError("AS {AsNumber} defines more than one IPv6 networks space.", asNumber);

        #endregion

        foreach (var router in @as.Routers)
            if (!currentAs.Routers.TryAdd(router.Key, router.Value))
                this.LogError("Router {RouterName} of AS number {AsNumber} is defined more than once.",
                    router.Key,
                    asNumber);
    }
}