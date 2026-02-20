namespace RouterQuack.Core;

/// <summary>
/// Step of the pipeline.
/// </summary>
public interface IStep
{
    protected internal bool ErrorsOccurred { get; set; }

    protected internal string? BeginMessage { get; }

    protected internal ILogger Logger { get; }

    protected internal Context Context { get; }
}

/// <summary>
/// Indicate that a step has failed.
/// </summary>
public class StepException : Exception
{
}