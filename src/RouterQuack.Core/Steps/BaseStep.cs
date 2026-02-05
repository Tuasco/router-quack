using RouterQuack.Core.Models;

namespace RouterQuack.Core.Steps;

public abstract class BaseStep() : IStep
{
    /// <summary>
    /// Whether errors have been generated during this step.
    /// </summary>
    protected bool ErrorsOccured { get; set; } = false;


    public virtual void Execute(ICollection<As> asses)
    {
        if (ErrorsOccured)
            throw new StepException();
    }
}