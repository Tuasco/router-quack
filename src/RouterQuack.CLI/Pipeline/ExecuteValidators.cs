using Microsoft.Extensions.DependencyInjection;
using RouterQuack.Core.Validators;
using Serilog;

namespace RouterQuack.CLI.Pipeline;

public class ExecuteValidators(IServiceProvider di) : IPipeline
{
    private readonly Context _context = di.GetRequiredService<Context>();

    public void Next()
    {
        Log.Information("Validating intent...");

        _context.ExecuteStep(di.GetRequiredKeyedService<IValidator>(nameof(NoDuplicateIpAddress)))
            .ExecuteStep(di.GetRequiredKeyedService<IValidator>(nameof(NoDuplicateLoopbackAddress)))
            .ExecuteStep(di.GetRequiredKeyedService<IValidator>(nameof(NoDuplicateRouterName)))
            .ExecuteStep(di.GetRequiredKeyedService<IValidator>(nameof(NoExternalRouterWithoutAddress)))
            .ExecuteStep(di.GetRequiredKeyedService<IValidator>(nameof(ValidBgpRelationships)))
            .ExecuteStep(di.GetRequiredKeyedService<IValidator>(nameof(ValidLoopbackAddresses)))
            .ExecuteStep(di.GetRequiredKeyedService<IValidator>(nameof(ValidLoopbackSpaces)))
            .ExecuteStep(di.GetRequiredKeyedService<IValidator>(nameof(ValidNetworkSpaces)))
            .ExecuteStep(di.GetRequiredKeyedService<IValidator>(nameof(ValidVrfReferences)))
            .ExecuteStep(di.GetRequiredKeyedService<IValidator>(nameof(WarningWhenAdditionalConfig)))
            .ExecuteStep(di.GetRequiredKeyedService<IValidator>(nameof(Ipv4SetWhenLdpIs)));
    }
}