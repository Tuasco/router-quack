using Microsoft.Extensions.DependencyInjection;
using RouterQuack.CLI.Pipeline;
using RouterQuack.CLI.Startup;
using RouterQuack.Core;
using RouterQuack.Core.Processors;
using Serilog;

// Dependency injection
using var serviceScope = DependencyInjection.CreateServiceScope(args);
var di = serviceScope.ServiceProvider;
var context = di.GetRequiredService<Context>();

// Pipeline
try
{
    // Read intent file
    new ReadIntentFiles(di).Next();

    // Resolve neighbours first
    context.ExecuteStep(di.GetRequiredKeyedService<IProcessor>(nameof(ResolveNeighbours)));

    // Execute validators
    new ExecuteValidators(di).Next();

    // Execute other processors
    new ExecuteProcessors(di).Next();

    // Stop here if dry run
    if (context.DryRun)
    {
        Log.Information("Skipping config files generation and deployment...");
        return;
    }

    // Write configs
    new WriteConfigs(di).Next();

    // Deploy configs
    new DeployConfigs(di).Next();

    Log.Information("Processing complete.");
}
catch (StepException)
{
    GracefulExit.OnStepException();
}
catch (Exception e)
{
    GracefulExit.OnUnhandledException(e, context);
}
finally
{
    GracefulExit.OnDebugEnabled(context);
}