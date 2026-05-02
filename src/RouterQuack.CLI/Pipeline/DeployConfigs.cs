using Microsoft.Extensions.DependencyInjection;
using RouterQuack.Core.ConfigDeployers;
using Serilog;

namespace RouterQuack.CLI.Pipeline;

public class DeployConfigs(IServiceProvider di) : IPipeline
{
    private readonly Context _context = di.GetRequiredService<Context>();

    public void Next()
    {
        Log.Information("Deploying configurations...");
        _context.ExecuteStep(di.GetRequiredService<IConfigDeployer>());
    }
}