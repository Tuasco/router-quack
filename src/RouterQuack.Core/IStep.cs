using Microsoft.Extensions.Logging;

namespace RouterQuack.Core;

/// <summary>
/// Step of the pipeline.
/// </summary>
public interface IStep
{
    protected internal bool ErrorsOccurred { get; set; }

    public ILogger Logger { get; set; }
}

/// <summary>
/// Indicate that a step has failed.
/// </summary>
public class StepException : Exception
{
}