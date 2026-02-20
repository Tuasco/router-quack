namespace RouterQuack.Core.Processors;

/// <summary>
/// Implemented by each step in the pipeline.
/// </summary>
public interface IProcessor : IStep
{
    /// <summary>
    /// Process a list of <see cref="As"/>.
    /// </summary>
    public void Process();
}