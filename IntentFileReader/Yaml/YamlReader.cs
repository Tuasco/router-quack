using System.Data;
using System.Text.RegularExpressions;
using RouterQuack.Models;
using RouterQuack.Startup;
using RouterQuack.Utils;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlAs = RouterQuack.Models.Yaml.As;

namespace RouterQuack.IntentFileReader.Yaml;

public partial class YamlReader(INetworkUtils networkUtils,
    IArgumentsParser argumentsParser,
    IDisplayUtils displayUtils) : IIntentFileReader
{
    [GeneratedRegex(@"\.ya?ml$")]
    private static partial Regex YamlEnding();
    
    // Throws DuplicateNameException when AS defined multiple times
    // Throws InvalidDataException when interface points to undefined neighbour
    public ICollection<As> ReadFiles()
    {
        displayUtils.Print("Parsing intent file(s)...", TextStyle.Title);
        var asDict = new Dictionary<int, YamlAs>();
        
        foreach (var path in argumentsParser.FilePaths)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"File {path} not found, skipping.");

            if (!YamlEnding().IsMatch(path))
            {
                displayUtils.Print($"File {path} is not YAML, skipping.",
                    textStyle: TextStyle.Warning,
                    verbosity: VerbosityLevel.Detailed);
                continue;
            }
            
            displayUtils.Print($"Reading file {path}", verbosity: VerbosityLevel.Detailed);
            
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
        
        displayUtils.Print($"Found {asDict.Count} ASs", verbosity:  VerbosityLevel.Normal);
        var asCollection = YamlAsToAs(asDict);
        return asCollection;
    }
}