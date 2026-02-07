using Microsoft.Extensions.Logging;

namespace RouterQuack.Core.Steps;

public interface IErrorCollector
{
    public bool ErrorsOccurred { get; set; }

    public ILogger Logger { get; set; }
}