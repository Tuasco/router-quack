using System.CommandLine;

namespace RouterQuack.CLI.Startup;

public class ArgumentsParser : IArgumentsParser
{
    public required string[] FilePaths  { get; set; }
    
    public required string  OutputDirectoryPath { get; set; }
    
    public required VerbosityLevel Verbosity { get; set; }
    
    public bool DryRun { get; set; }
    
    
    public static ArgumentsParser CreateFromArgs(string[] args)
    {
        // Add file option
        Option<IEnumerable<FileInfo>> fileOption = new("--file", "-f")
        {
            Description = "Intent file to process.",
            Arity = ArgumentArity.OneOrMore,
            Required = true,
            DefaultValueFactory = _ => [new ("examples/default.yaml")],
            AllowMultipleArgumentsPerToken = true
        };
        
        // Add output option
        Option<DirectoryInfo> outputOption = new("--output", "-o")
        {
            Description = "Output directory. Router configuration files will be generated here.",
            Arity = ArgumentArity.ExactlyOne,
            Required = true,
            DefaultValueFactory = _ => new ("output")
        };
        
        // Add verbosity options
        Option<bool> quietOption = new("--quiet", "-q")
        {
            Description = "Set verbosity to quiet. Still shows warnings and errors",
        };
        Option<bool> verboseOption = new("--verbose", "-v") 
        { 
            Description = "Set verbosity to detailed."
        };
        
        // Add dry run option
        Option<bool> dryRunOption = new("--dry-run", "-n")
        {
            Description = "Dry run. When set, nothing will be written to the routers or the filesystem."
        };
        
        var rootCommand = new RootCommand("Generate router configuration files from user-friendly intent files");
        rootCommand.Options.Add(fileOption);
        rootCommand.Options.Add(outputOption);
        rootCommand.Options.Add(verboseOption);
        rootCommand.Options.Add(quietOption);
        rootCommand.Options.Add(dryRunOption);
        
        ArgumentsParser? result = null;
        rootCommand.SetAction(parseResult =>
        {
            // Intent files
            var filePaths = parseResult.GetRequiredValue(fileOption).Select(f => f.FullName).ToArray();
            
            // Output directory
            var outputDirectoryPath = parseResult.GetRequiredValue(outputOption).FullName;
            
            // Determine verbosity level
            var verbosityLevel = parseResult.GetValue(verboseOption)
                ? VerbosityLevel.Detailed
                : parseResult.GetValue(quietOption)
                    ? VerbosityLevel.Quiet
                    : VerbosityLevel.Normal;
            
            Console.WriteLine(verbosityLevel.ToString());
            
            // Dry run
            var dryRun = parseResult.GetValue(dryRunOption);
            
            result = new ArgumentsParser
            {
                FilePaths = filePaths,
                OutputDirectoryPath = outputDirectoryPath,
                Verbosity = verbosityLevel,
                DryRun = dryRun
            };
        });

	    rootCommand.Parse(args).Invoke();
        
        // If result is null, probably because user passer --help or --version.
        if (result == null)
            Environment.Exit(0);
        
        return result;
    }
}