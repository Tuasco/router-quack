using RouterQuack.Core.Models;

namespace RouterQuack.Core.Processors;

/// <summary>
/// Implemented by each step in the pipeline.
/// </summary>
public interface IProcessor : IStep
{
    /// <summary>
    /// Process a list of <see cref="As"/>.
    /// </summary>
    /// <param name="asses">A populated collection of As objects.</param>
    public void Process(ICollection<As> asses);
}