using Microsoft.Extensions.DependencyInjection;
using RouterQuack.CLI.Startup;
using RouterQuack.Core;
using RouterQuack.Core.Extensions;
using RouterQuack.Core.IntentFileParsers;
using RouterQuack.Core.Processors;
using RouterQuack.Core.Validators;
using Serilog;

// Dependency injection
using var serviceScope = DependencyInjection.CreateServiceScope(args);
var di = serviceScope.ServiceProvider;
var context = di.GetRequiredService<Context>();

// Pipeline
try
{
    // Read intent file
    context.ExecuteStep(di.GetRequiredService<IIntentFileParser>());

    // Resolve neighbours first
    context.ExecuteStep(di.GetRequiredKeyedService<IProcessor>(nameof(ResolveNeighbours)));

    // Execute validators
    Log.Information("Validating intent...");
    context.ExecuteStep(di.GetRequiredKeyedService<IValidator>(nameof(NoDuplicateIpAddress)))
        .ExecuteStep(di.GetRequiredKeyedService<IValidator>(nameof(NoDuplicateLoopbackAddress)))
        .ExecuteStep(di.GetRequiredKeyedService<IValidator>(nameof(NoDuplicateRouterName)))
        .ExecuteStep(di.GetRequiredKeyedService<IValidator>(nameof(NoExternalRouterWithoutAddress)))
        .ExecuteStep(di.GetRequiredKeyedService<IValidator>(nameof(ValidBgpRelationships)))
        .ExecuteStep(di.GetRequiredKeyedService<IValidator>(nameof(ValidLoopbackAddresses)))
        .ExecuteStep(di.GetRequiredKeyedService<IValidator>(nameof(ValidLoopbackSpaces)))
        .ExecuteStep(di.GetRequiredKeyedService<IValidator>(nameof(ValidNetworkSpaces)));

    // Execute other processors
    context.ExecuteStep(di.GetRequiredKeyedService<IProcessor>(nameof(GenerateLinkAddresses)))
        .ExecuteStep(di.GetRequiredKeyedService<IProcessor>(nameof(GenerateLoopbackAddresses)))
        .ExecuteStep(di.GetRequiredKeyedService<IProcessor>(nameof(PopulateRouterIds)));
}
catch (StepException)
{
    Log.Fatal("Exited with errors. Nothing changed.");
    Log.Debug("ASs summary:\n{Summary}", context.Asses.Summary());
    Environment.Exit(1);
}
catch (Exception e)
{
    Log.Fatal("Unhandled exception occured.");
    Log.Debug("Exception:\n{Exception}", e);
    Log.Debug("ASs summary:\n{Summary}", context.Asses.Summary());
    Environment.Exit(2);
}

Log.Information("Processing complete.");