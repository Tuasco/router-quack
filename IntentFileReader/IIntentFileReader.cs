namespace RouterQuack.IntentFileReader;

public interface IIntentFileReader
{
    public ICollection<Models.As> ReadFiles(string[] paths);
}