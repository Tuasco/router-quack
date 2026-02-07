using RouterQuack.Core.Models;

namespace RouterQuack.Core.Steps;

/// <summary>
/// Implemented by each step in the pipeline.
/// </summary>
public interface IStep : IErrorCollector
{
    /// <summary>
    /// Execute a step of the pipeline.
    /// </summary>
    /// <param name="asses">A populated collection of As objects.</param>
    public void Execute(ICollection<As> asses);
}

/// <summary>
/// Exception indicating that a step has failed.
/// </summary>
public class StepException : Exception
{
}