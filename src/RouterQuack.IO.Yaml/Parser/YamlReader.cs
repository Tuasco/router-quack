using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
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
    public ILogger Logger { get; set; } = logger;

    /// <summary>
    /// Parse intent files and return a corresponding collection of As objects.
    /// </summary>
    /// <param name="filePaths">Paths of intent files to parse.</param>
    /// <returns>New collection of As objects.</returns>
    public ICollection<As> ReadFiles(string[] filePaths)
    {
        logger.LogInformation("Parsing intent file(s)...");
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
            {
                if (asDict.ContainsKey(@as.Key))
                {
                    logger.LogError("Duplicate AS number {AsNumber}", @as.Key);
                    ErrorsOccurred = true;
                }

                asDict.Add(@as.Key, @as.Value);
            }
        }

        logger.LogDebug("Found {AsNumber} ASs", asDict.Count);
        var asCollection = YamlAsToCoreAs(asDict);
        return asCollection;
    }
}