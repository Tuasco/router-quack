using RouterQuack.Startup;
using Spectre.Console;

namespace RouterQuack.Utils;

public interface IDisplayUtils
{
    public void Print(string text,
        TextStyle textStyle = TextStyle.Normal,
        VerbosityLevel verbosity =  VerbosityLevel.Minimal,
        bool lineFeed = true);
}

public class DisplayUtils(IArgumentsParser argumentsParser) : IDisplayUtils
{
    private readonly Style _titleStyle = new (decoration: Decoration.Bold |  Decoration.Italic);
    
    private readonly Style _normalStyle = new ();
    
    private readonly Style _warningStyle = new (
        ConsoleColor.DarkYellow, decoration: Decoration.Bold);
    
    private readonly Style _errorStyle = new (
        ConsoleColor.Red, decoration: Decoration.Bold);

    private bool _firstTitle = true;
    
    public void Print(string text, TextStyle textStyle, VerbosityLevel verbosity, bool lineFeed)
    {
        if (argumentsParser.Verbosity == VerbosityLevel.Quiet)
            return;
        
        if (argumentsParser.Verbosity < verbosity)
            return;

        var style = textStyle switch
        {
            TextStyle.Title => _titleStyle,
            TextStyle.Warning => _warningStyle,
            TextStyle.Error => _errorStyle,
            _ => _normalStyle
        };

        // Append line feed
        if (lineFeed)
            text += "\n";

        // Prepend padding if not title nor unformatted
        if (textStyle != TextStyle.Title && textStyle != TextStyle.Unformatted)
            text = text.Insert(0, "- ");
        
        // Prepend line feed
        if (textStyle == TextStyle.Title && !_firstTitle)
            text = text.Insert(0, "\n");
        if (textStyle == TextStyle.Title)
            _firstTitle = false;

        AnsiConsole.Write(new Markup(text, style));
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
    Title,
    Normal,
    Warning,
    Error,
    Unformatted
}