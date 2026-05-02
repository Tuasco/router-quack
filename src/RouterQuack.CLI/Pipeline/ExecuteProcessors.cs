using Microsoft.Extensions.DependencyInjection;
using RouterQuack.Core.Processors;

namespace RouterQuack.CLI.Pipeline;

public class ExecuteProcessors(IServiceProvider di) : IPipeline
{
    private readonly Context _context = di.GetRequiredService<Context>();

    public void Next()
    {
        _context.ExecuteStep(di.GetRequiredKeyedService<IProcessor>(nameof(GenerateLinkAddresses)))
            .ExecuteStep(di.GetRequiredKeyedService<IProcessor>(nameof(GenerateLoopbackAddresses)))
            .ExecuteStep(di.GetRequiredKeyedService<IProcessor>(nameof(PopulateRouterIds)))
            .ExecuteStep(di.GetRequiredKeyedService<IProcessor>(nameof(PopulateVrfRdRt)))
            .ExecuteStep(di.GetRequiredKeyedService<IProcessor>(nameof(ToggleIbgp)));
    }
}