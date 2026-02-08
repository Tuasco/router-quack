using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using RouterQuack.Core.Steps;

#pragma warning disable CA2254
// ReSharper disable ConvertToExtensionBlock
// Roslyn has know issues with params in extension block. See https://github.com/dotnet/roslyn/issues/80024.

namespace RouterQuack.Core.Extensions;

public static class ErrorCollectorExtensions
{
    /// <summary>
    /// Log an error and set ErrorsOccurred to true.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="message">Message as a structured template.</param>
    /// <param name="args">Arguments to fill the message with.</param>
    public static void LogError(this IErrorCollector source,
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
    public static void LogWarning(this IErrorCollector source,
        [StructuredMessageTemplate] string message, params object?[] args)
    {
        source.Logger.LogWarning(message, args);
    }
}