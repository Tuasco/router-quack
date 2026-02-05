using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using RouterQuack.Core.Models;
using RouterQuack.Core.Utils;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlAs = RouterQuack.IO.Yaml.Models.As;

namespace RouterQuack.IO.Yaml;

public partial class YamlReader(INetworkUtils networkUtils, 
    IRouterUtils routerUtils,
    ILogger<YamlReader> logger) : IIntentFileReader
{
    [GeneratedRegex(@"\.ya?ml$")]
    private static partial Regex YamlEnding();
    
    // Throws DuplicateNameException when AS defined multiple times
    // Throws InvalidDataException when interface points to undefined neighbour
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
                    throw new DuplicateNameException($"Duplicate AS number {@as.Key}");
                
                asDict.Add(@as.Key, @as.Value);
            }
        }
        
        logger.LogDebug("Found {AsNumber} ASs", asDict.Count);
        var asCollection = YamlAsToCoreAs(asDict);
        return asCollection;
    }
}