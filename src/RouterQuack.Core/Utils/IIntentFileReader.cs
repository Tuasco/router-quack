using RouterQuack.Core.Models;

namespace RouterQuack.Core.Utils;

public interface IIntentFileReader
{
    public ICollection<As> ReadFiles(string[] filePaths);
}