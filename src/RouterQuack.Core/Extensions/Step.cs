using System.ComponentModel;
using System.Text;
using JetBrains.Annotations;

// Roslyn has know issues with params in extension block. See https://github.com/dotnet/roslyn/issues/80024.
// ReSharper disable ConvertToExtensionBlock
#pragma warning disable CA2254

namespace RouterQuack.Core.Extensions;

public static class StepExtensions
{
    /// <summary>
    /// Log a custom message for an interface.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="message">Message to log.</param>
    /// <param name="interface"><see cref="Interface"/> to extract data from.</param>
    /// <param name="logLevel"><see cref="LogLevel"/> to log with.</param>
    /// <remarks>This method adds a point at the end of the line.</remarks>
    public static void Log(this IStep source, Interface @interface, string message, LogLevel logLevel = LogLevel.Error)
    {
        var logMessage = new StringBuilder()
            .AppendJoin(' ', "Interface {InterfaceName} of router {RouterName} in AS number {AsNumber}:", message)
            .Append('.').ToString();

        source.Log(logLevel, logMessage,
            @interface.Name, @interface.ParentRouter.Name, @interface.ParentRouter.ParentAs.Number);
    }

    /// <summary>
    /// Log a custom message for a Router.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="message">Message to log.</param>
    /// <param name="router"><see cref="Router"/> to extract data from.</param>
    /// <param name="logLevel"><see cref="LogLevel"/> to log with.</param>
    /// <remarks>This method adds a point at the end of the line.</remarks>
    public static void Log(this IStep source, Router router, string message, LogLevel logLevel = LogLevel.Error)
    {
        var logMessage = new StringBuilder()
            .AppendJoin(' ', "Router {RouterName} in AS number {AsNumber}:", message)
            .Append('.').ToString();

        source.Log(logLevel, logMessage,
            router.Name, router.ParentAs.Number);
    }

    /// <summary>
    /// Log a custom message for an AS.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="message">Message to log.</param>
    /// <param name="as"><see cref="As"/> to extract data from.</param>
    /// <param name="logLevel"><see cref="LogLevel"/> to log with.</param>
    /// <remarks>This method adds a point at the end of the line.</remarks>
    public static void Log(this IStep source, As @as, string message, LogLevel logLevel = LogLevel.Error)
    {
        var logMessage = new StringBuilder()
            .AppendJoin(' ', "AS number {AsNumber}:", message)
            .Append('.').ToString();

        source.Log(logLevel, logMessage,
            @as.Number);
    }

    /// <summary>
    /// Log a message with a custom <see cref="LogLevel"/>.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="level"><see cref="LogLevel"/> to log with.</param>
    /// <param name="message">Message as a structured template.</param>
    /// <param name="args">Arguments to fill the message with.</param>
    /// <exception cref="InvalidEnumArgumentException">Unsupported <see cref="LogLevel"/>.</exception>
    private static void Log(this IStep source, LogLevel level,
        [StructuredMessageTemplate] string message, params object?[] args)
    {
        switch (level)
        {
            case LogLevel.Debug:
                source.Logger.LogDebug(message, args);
                break;

            case LogLevel.Warning:
                source.LogWarning(message, args);
                break;

            case LogLevel.Error:
                source.LogError(message, args);
                break;

            case LogLevel.Trace:
            case LogLevel.Information:
            case LogLevel.Critical:
            case LogLevel.None:
            default:
                throw new InvalidEnumArgumentException("Invalid log level.");
        }
    }

    /// <summary>
    /// Log an error and set ErrorsOccurred to true.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="message">Message as a structured template.</param>
    /// <param name="args">Arguments to fill the message with.</param>
    public static void LogError(this IStep source,
        [StructuredMessageTemplate] string message, params object?[] args)
    {
        source.Logger.LogError(message, args);
        source.ErrorsOccurred = true;
    }

    /// <summary>
    /// Log a warning.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="message">Message as a structured template.</param>
    /// <param name="args">Arguments to fill the message with.</param>
    public static void LogWarning(this IStep source,
        [StructuredMessageTemplate] string message, params object?[] args)
    {
        source.Logger.LogWarning(message, args);

        if (source.Context.Strict)
            source.ErrorsOccurred = true;
    }
}