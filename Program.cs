using Microsoft.Extensions.DependencyInjection;
using RouterQuack.Extensions;
using RouterQuack.IntentFileReader;
using RouterQuack.Startup;
using RouterQuack.Steps;

// Dependency injection
using var serviceScope = DependencyInjection.CreateServiceScope(args);

// Arguments parser
var argumentsParser = serviceScope.ServiceProvider.GetRequiredService<IArgumentsParser>();
argumentsParser.Parse(args);

// Pipeline
var intentFileReader = serviceScope.ServiceProvider.GetRequiredService<IIntentFileReader>();
var step1ResolveNeighbours = serviceScope.ServiceProvider.GetRequiredKeyedService<IStep>(nameof(Step1ResolveNeighbours));
var step2RunChecks = serviceScope.ServiceProvider.GetRequiredKeyedService<IStep>(nameof(Step2RunChecks));

var asses = intentFileReader.ReadFiles(argumentsParser.FilePaths)
    .ExecuteStep(step1ResolveNeighbours)
    .ExecuteStep(step2RunChecks);

asses.Display();