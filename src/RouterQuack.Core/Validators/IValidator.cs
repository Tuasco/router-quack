using RouterQuack.Core.Models;

namespace RouterQuack.Core.Validators;

public interface IValidator : IStep
{
    /// <summary>
    /// Validate a list of <see cref="As"/>.
    /// </summary>
    /// <param name="asses">A populated collection of As objects.</param>
    public void Validate(ICollection<As> asses);
}