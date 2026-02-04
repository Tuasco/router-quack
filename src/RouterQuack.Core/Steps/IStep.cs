using RouterQuack.Core.Models;

namespace RouterQuack.Core.Steps;

public interface IStep
{
    public void Execute(ICollection<As> asses);
}