using System.CommandLine;
using System.CommandLine.Parsing;

namespace RouterQuack.Startup;

public class ArgumentParser
{
    public required string[] FilePaths  { get; set; }
    
    private readonly RootCommand rootCommand;
    
    
    public ArgumentParser()
    {
        // Add file option
        Option<IEnumerable<FileInfo>> fileOption = new("--file", "-f")
        {
            Description = "Pass an intent file",
            DefaultValueFactory = _ => [new FileInfo("Examples/default.yaml")],
            Arity =  ArgumentArity.OneOrMore,
            AllowMultipleArgumentsPerToken = true
        };

        rootCommand = new("Generate router configuration files from user-friendly intent files");
        rootCommand.Options.Add(fileOption);
        rootCommand.SetAction(parseResult => 
            FilePaths = (parseResult.GetValue(fileOption) ?? []).Select(f => f.FullName).ToArray());
    }

    // Return whether parsing was successful
    public IReadOnlyList<ParseError> Parse(string[] args)
    {
        var parseResult = rootCommand.Parse(args);
        parseResult.Invoke();
        
        return parseResult.Errors;
    }
}