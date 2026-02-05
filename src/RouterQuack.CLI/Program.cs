using Microsoft.Extensions.DependencyInjection;
using RouterQuack.CLI.Startup;
using RouterQuack.Core.Extensions;
using RouterQuack.Core.Steps;
using RouterQuack.Core.Utils;
using Serilog;

// Dependency injection
using var serviceScope = DependencyInjection.CreateServiceScope(args);

// Arguments parser
var argumentsParser = serviceScope.ServiceProvider.GetRequiredService<IArgumentsParser>();

// Pipeline
var intentFileReader = serviceScope.ServiceProvider.GetRequiredService<IIntentFileReader>();
var step1ResolveNeighbours =
    serviceScope.ServiceProvider.GetRequiredKeyedService<IStep>(nameof(Step1ResolveNeighbours));
var step2RunChecks = serviceScope.ServiceProvider.GetRequiredKeyedService<IStep>(nameof(Step2RunChecks));

var asses = intentFileReader.ReadFiles(argumentsParser.FilePaths);

try
{
    asses.ExecuteStep(step1ResolveNeighbours)
        .ExecuteStep(step2RunChecks);
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