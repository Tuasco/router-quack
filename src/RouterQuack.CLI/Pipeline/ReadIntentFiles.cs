using Microsoft.Extensions.DependencyInjection;
using RouterQuack.Core.IntentFileParsers;

namespace RouterQuack.CLI.Pipeline;

public class ReadIntentFiles(IServiceProvider di) : IPipeline
{
    private readonly Context _context = di.GetRequiredService<Context>();

    public void Next()
    {
        _context.ExecuteStep(di.GetRequiredService<IIntentFileParser>());
    }
}