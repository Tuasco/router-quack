using System.Reflection;
using System.Text.RegularExpressions;
using Humanizer;
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
    ILogger<YamlParser> logger,
    Context context,
    NetworkUtils networkUtils,
    AsUtils asUtils,
    RouterUtils routerUtils,
    InterfaceUtils interfaceUtils) : IIntentFileParser
{
    [GeneratedRegex(@"\.ya?ml$")]
    private static partial Regex YamlEnding();

    public bool ErrorsOccurred { get; set; }
    public string BeginMessage => "Parsing intent file(s)";
    public ILogger Logger { get; } = logger;
    public Context Context { get; } = context;


    public void ReadFiles(string[] filePaths)
    {
        var asDict = new Dictionary<int, YamlAs>();

        foreach (var path in filePaths)
        {
            if (!File.Exists(path))
            {
                this.LogWarning("File {Path} not found, skipping.", path);
                continue;
            }

            if (!YamlEnding().IsMatch(path))
            {
                this.LogWarning("File {Path} is not YAML, skipping.", path);
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
        YamlAsToCoreAs(asDict, Context.Asses);

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
    /// This method accepts duplicate ASs granted they do not have duplicate properties with different values,
    /// or duplicates routers.
    /// </remarks>
    private void AddAs(int asNumber, YamlAs @as, Dictionary<int, YamlAs> asDict)
    {
        if (asDict.TryAdd(asNumber, @as))
            return;

        // Check no properties are defined twice and differently
        var currentAs = asDict[asNumber];
        foreach (var prop in YamlAsProperties)
        {
            var val1 = prop.GetValue(currentAs);
            var val2 = prop.GetValue(@as);

            if (val1 != null && val2 != null && !val1.Equals(val2))
                this.LogError("AS {AsNumber} defines more than one {Property}.", asNumber, prop.Name.Underscore());
        }

        foreach (var router in @as.Routers)
            if (!currentAs.Routers.TryAdd(router.Key, router.Value))
                this.LogError("Router {RouterName} of AS number {AsNumber} is defined more than once.",
                    router.Key,
                    asNumber);
    }

    private static readonly PropertyInfo[] YamlAsProperties = typeof(YamlAs)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.Name != nameof(YamlAs.Routers))
        .ToArray();
}