using System.Data;
using System.Text.RegularExpressions;
using RouterQuack.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlAs = RouterQuack.Models.Yaml.As;
using static System.Console;

namespace RouterQuack.IntentFileReader.Yaml;

public partial class YamlReader : IIntentFileReader
{
    [GeneratedRegex(@"\.ya?ml$")]
    private static partial Regex YamlEnding();
    
    // Throws DuplicateNameException when AS defined multiple times
    // Throws InvalidDataException when interface points to undefined neighbour
    public ICollection<As> ReadFiles(string[] paths)
    {
        var asDict = new Dictionary<int, YamlAs>();
        
        foreach (var path in paths)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"File {path} not found");

            if (!YamlEnding().IsMatch(path))
            {
                WriteLine($"File {path} is not a Yaml file. Skipping");
                continue;
            }
            
            WriteLine($"Reading file {path}");
            
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
        
        WriteLine($"Found {asDict.Count} ASs");
        var asCollection = YamlAsToAs(asDict);
        WriteLine("Intent files parsed successfully");
        return asCollection;
    }
}