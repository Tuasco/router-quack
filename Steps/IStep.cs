using RouterQuack.Models;

namespace RouterQuack.Steps;

public interface IStep
{
    public void Execute(ICollection<As> asses);
}