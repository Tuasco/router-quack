using System.CommandLine;
using RouterQuack.Utils;

namespace RouterQuack.Startup;

public interface IArgumentsParser
{
    public string[] FilePaths  { get; set; }
    
    public string OutputDirectoryPath { get; set; }
    
    public VerbosityLevel Verbosity { get; set; }
    
    public bool DryRun { get; set; }

    public void Parse(string[] args);
}

public class ArgumentsParser : IArgumentsParser
{
    public required string[] FilePaths  { get; set; }
    
    public required string  OutputDirectoryPath { get; set; }
    
    public required VerbosityLevel Verbosity { get; set; }
    
    public bool DryRun { get; set; }

    private readonly RootCommand _rootCommand;
    
    
    public ArgumentsParser()
    {
        // Add file option
        Option<IEnumerable<FileInfo>> fileOption = new("--file", "-f")
        {
            Description = "Intent file to process.",
            DefaultValueFactory = _ => [new ("Examples/default.yaml")],
            Arity = ArgumentArity.OneOrMore,
            Required = true,
            AllowMultipleArgumentsPerToken = true
        };
        
        // Add output option
        Option<DirectoryInfo> outputOption = new("--output", "-o")
        {
            Description = "Ouput directory. Router configuration files will be generated here.",
            DefaultValueFactory = _ => new ("Output"),
            Arity = ArgumentArity.ExactlyOne,
            Required = true
        };
        
        // Add verbosity options
        Option<string> verbosityOption = new("--verbosity", "-v")
        {
            // ReSharper disable once StringLiteralTypo
            Description = "Output verbosity level. Allowed values are q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic].",
            Arity = ArgumentArity.ZeroOrOne,
            DefaultValueFactory = _ => "normal"
        };

        // Add -q as a separate option for quiet verbosity.
        Option<bool> quietOption = new("--quiet", "-q")
        {
            Description = "Set verbosity to quiet (shorthand for --verbosity quiet)."
        };
        
        // Add dry run option
        Option<bool> dryRunOption = new("--dry-run", "-n")
        {
            Description = "Dry run. When set, nothing will be written to the routers or the filesystem."
        };
        
        _rootCommand = new("Generate router configuration files from user-friendly intent files");
        _rootCommand.Options.Add(fileOption);
        _rootCommand.Options.Add(outputOption);
        _rootCommand.Options.Add(verbosityOption);
        _rootCommand.Options.Add(quietOption);
        _rootCommand.Options.Add(dryRunOption);
        
        _rootCommand.SetAction(parseResult =>
        {
            // Files
            FilePaths = (parseResult.GetRequiredValue(fileOption)).Select(f => f.FullName).ToArray();
            
            // Output
            OutputDirectoryPath = parseResult.GetRequiredValue(outputOption).FullName;

            // Verbosity
            var verbosityString = parseResult.GetValue(quietOption) 
                ? "quiet"
                : parseResult.GetRequiredValue(verbosityOption);
            
            Verbosity = verbosityString switch
            {
                "quiet" or "q" => VerbosityLevel.Quiet,
                "minimal" or "m" => VerbosityLevel.Minimal,
                "detailed" or "d" => VerbosityLevel.Detailed,
                "diagnostic" or "diag" => VerbosityLevel.Diagnostic,
                _ => VerbosityLevel.Normal
            };
            
            // Dry run
            DryRun = parseResult.GetValue(dryRunOption);
        });
    }

    public void Parse(string[] args)
    {
        var parseResult = _rootCommand.Parse(args);
        parseResult.Invoke();

        if (parseResult.Errors.Any())
            throw new ArgumentException(parseResult.Errors.First().Message);
    }
}