using Microsoft.Extensions.DependencyInjection;
using RouterQuack.Extensions;
using RouterQuack.IntentFileReader;
using RouterQuack.Startup;
using RouterQuack.Steps;
using RouterQuack.Utils;

// Dependency injection
using var serviceScope = DependencyInjection.CreateServiceScope(args);

// Display
var displayUtils = serviceScope.ServiceProvider.GetRequiredService<IDisplayUtils>();
displayUtils.Print("Starting parsing...", verbosity: VerbosityLevel.Normal);

// Arguments parser
var argumentsParser = serviceScope.ServiceProvider.GetRequiredService<IArgumentsParser>();
argumentsParser.Parse(args);

// Pipeline
var intentFileReader = serviceScope.ServiceProvider.GetRequiredService<IIntentFileReader>();
var step1ResolveNeighbours = serviceScope.ServiceProvider.GetRequiredKeyedService<IStep>(nameof(Step1ResolveNeighbours));
var step2RunChecks = serviceScope.ServiceProvider.GetRequiredKeyedService<IStep>(nameof(Step2RunChecks));

var asses = intentFileReader.ReadFiles()
    .ExecuteStep(step1ResolveNeighbours)
    .ExecuteStep(step2RunChecks);

displayUtils.Print("In-memory configuration :", TextStyle.Title, VerbosityLevel.Diagnostic);
displayUtils.Print(asses.Summary(), TextStyle.Unformatted, VerbosityLevel.Diagnostic);