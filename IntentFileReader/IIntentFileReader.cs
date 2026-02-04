using RouterQuack.Models;

namespace RouterQuack.IntentFileReader;

public interface IIntentFileReader
{
    public ICollection<As> ReadFiles();
}