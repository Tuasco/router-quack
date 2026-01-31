namespace RouterQuack.IntentFileReader;

public interface IIntentFileReader
{
    public ICollection<RouterQuack.Models.As> ReadFiles(string[] paths);
}