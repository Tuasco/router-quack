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

// Arguments parser
var argumentsParser = di.GetRequiredService<IArgumentsParser>();

// Pipeline
var intentFileReader = di.GetRequiredService<IIntentFileParser>();
var step1ResolveNeighbours = di.GetRequiredKeyedService<IProcessor>(nameof(ResolveNeighbours));
var step3GenerateIpAddresses = di.GetRequiredKeyedService<IProcessor>(nameof(GenerateLinkAddresses));
var step4GenerateLoopbackAddresses = di.GetRequiredKeyedService<IProcessor>(nameof(GenerateLoopbackAddresses));

var asses = intentFileReader.ReadFiles(argumentsParser.FilePaths);
try
{
    // Resolve neighbours first
    asses.ExecuteStep(step1ResolveNeighbours);

    // Execute validators
    Log.Information("Validating intent...");
    asses.ExecuteStep(di.GetRequiredKeyedService<IValidator>(nameof(NoDuplicateIpAddress)))
        .ExecuteStep(di.GetRequiredKeyedService<IValidator>(nameof(NoDuplicateRouterNames)))
        .ExecuteStep(di.GetRequiredKeyedService<IValidator>(nameof(NoExternalRouterWithoutAddress)))
        .ExecuteStep(di.GetRequiredKeyedService<IValidator>(nameof(ValidBgpRelationships)))
        .ExecuteStep(di.GetRequiredKeyedService<IValidator>(nameof(ValidLoopbackAddresses)))
        .ExecuteStep(di.GetRequiredKeyedService<IValidator>(nameof(ValidNetworkSpaces)));

    // Execute other processors
    asses.ExecuteStep(step3GenerateIpAddresses)
        .ExecuteStep(step4GenerateLoopbackAddresses);
}
catch (StepException)
{
    Log.Information("Exited with errors. Nothing changed.");
    Log.Debug("ASs summary:\n{Summary}", asses.Summary());
    Environment.Exit(1);
}
catch (Exception e)
{
    Log.Error("Unhandled exception occured.");
    Log.Debug("Exception:\n{Exception}", e);
    Log.Debug("ASs summary:\n{Summary}", asses.Summary());
    Environment.Exit(2);
}

Log.Information("Processing complete.");