namespace RouterQuack.Core.Steps;

public interface IErrorCollector
{
    public bool ErrorsOccurred { get; set; }
}