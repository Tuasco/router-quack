using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using RouterQuack.Core.Steps;

namespace RouterQuack.Core.Extensions;

public static class ErrorCollectorExtensions
{
    extension(IErrorCollector source)
    {
        public void LogError([StructuredMessageTemplate] string message, params object?[] args)
        {
            source.Logger.LogError(message, args);
            source.ErrorsOccurred = true;
        }

        public void LogWarning([StructuredMessageTemplate] string message, params object?[] args)
        {
            source.Logger.LogError(message, args);
        }
    }
}