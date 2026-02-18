using RouterQuack.Core.Models;

namespace RouterQuack.Core.Validators;

public interface IValidator : IStep
{
    /// <summary>
    /// Validate a list of <see cref="As"/>.
    /// </summary>
    public void Validate();
}