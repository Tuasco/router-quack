using Microsoft.Extensions.DependencyInjection;
using RouterQuack.CLI.Startup;
using RouterQuack.Core.Extensions;
using RouterQuack.Core.Steps;
using Serilog;

// Dependency injection
using var serviceScope = DependencyInjection.CreateServiceScope(args);

// Arguments parser
var argumentsParser = serviceScope.ServiceProvider.GetRequiredService<IArgumentsParser>();

// Pipeline
var intentFileReader = serviceScope.ServiceProvider.GetRequiredService<IIntentFileParser>();
var step1ResolveNeighbours =
    serviceScope.ServiceProvider.GetRequiredKeyedService<IStep>(nameof(Step1ResolveNeighbours));
var step2RunChecks =
    serviceScope.ServiceProvider.GetRequiredKeyedService<IStep>(nameof(Step2RunChecks));
var step3GenerateIpAddresses =
    serviceScope.ServiceProvider.GetRequiredKeyedService<IStep>(nameof(Step3GenerateLinkAddresses));
var step4GenerateLoopbackAddresses =
    serviceScope.ServiceProvider.GetRequiredKeyedService<IStep>(nameof(Step4GenerateLoopbackAddresses));

var asses = intentFileReader.ReadFiles(argumentsParser.FilePaths);
if (intentFileReader.ErrorsOccurred)
{
    Log.Information("Exited with errors. Nothing changed.");
    Log.Debug("ASs summary:\n{Summary}", asses.Summary());
    Environment.Exit(1);
}

try
{
    asses.ExecuteStep(step1ResolveNeighbours)
        .ExecuteStep(step2RunChecks)
        .ExecuteStep(step3GenerateIpAddresses)
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