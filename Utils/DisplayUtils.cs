using RouterQuack.Startup;

namespace RouterQuack.Utils;

public interface IDisplayUtils
{
    public void Print(string text, TextStyle textStyle, VerbosityLevel verbosity);
}

public class DisplayUtils(IArgumentsParser argumentsParser) : IDisplayUtils
{
    public void Print(string text, TextStyle textStyle, VerbosityLevel verbosity)
    {
        if (argumentsParser.Verbosity < verbosity)
            return;
        
        Console.WriteLine(text);
    }
}

public enum VerbosityLevel
{
    Quiet,
    Minimal,
    Normal,
    Detailed,
    Diagnostic
}

public enum TextStyle
{
    Normal
}