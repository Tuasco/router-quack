using Microsoft.Extensions.DependencyInjection;
using RouterQuack.CLI.Startup;
using RouterQuack.Core;
using RouterQuack.Core.ConfigDeployers;
using RouterQuack.Core.ConfigFileWriters;
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
        .ExecuteStep(di.GetRequiredKeyedService<IValidator>(nameof(ValidNetworkSpaces)))
        .ExecuteStep(di.GetRequiredKeyedService<IValidator>(nameof(WarningWhenAdditionalConfig)));

    // Execute other processors
    context.ExecuteStep(di.GetRequiredKeyedService<IProcessor>(nameof(GenerateLinkAddresses)))
        .ExecuteStep(di.GetRequiredKeyedService<IProcessor>(nameof(GenerateLoopbackAddresses)))
        .ExecuteStep(di.GetRequiredKeyedService<IProcessor>(nameof(PopulateRouterIds)))
        .ExecuteStep(di.GetRequiredKeyedService<IProcessor>(nameof(ToggleIbgp)));

    // Stop here if dry run
    if (context.DryRun)
    {
        Log.Information("Skipping config files generation and deployment...");
        return;
    }

    // Write config files to output folder
    Log.Information("Generating config files...");

    if (!Directory.Exists(context.OutputDirectoryPath))
    {
        Log.Debug("Creating output directory.");
        Directory.CreateDirectory(context.OutputDirectoryPath);
    }

    context.ExecuteStep(di.GetRequiredKeyedService<IConfigFileWriter>(RouterBrand.Cisco));

    // Deploy configurations to GNS3 if any AS has deploy info
    context.ExecuteStep(di.GetRequiredService<IConfigDeployer>());
}
catch (StepException)
{
    Log.Fatal("Exited with errors. Nothing changed.");
    Environment.ExitCode = 1;
}
catch (Exception e)
{
    Log.Fatal("Unhandled exception occured.");
    Log.Debug("Exception:\n{Exception}", e);
    Log.Debug("ASs summary:\n{Summary}", context.Asses.Summary());
    Environment.ExitCode = 2;
}
finally
{
    if (context.DebugGraph)
        Console.WriteLine("ASs summary: {0}", context.Asses.Summary());
}