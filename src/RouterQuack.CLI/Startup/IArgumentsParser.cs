namespace RouterQuack.CLI.Startup;

public interface IArgumentsParser
{
    public string[] FilePaths  { get; set; }
    
    public string OutputDirectoryPath { get; set; }
    
    public VerbosityLevel Verbosity { get; set; }
    
    public bool DryRun { get; set; }
}

public enum VerbosityLevel
{
    Quiet,      // -q
    Normal,    // Default
    Detailed    // -v
}