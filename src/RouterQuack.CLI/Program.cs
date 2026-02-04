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
var step1ResolveNeighbours = serviceScope.ServiceProvider.GetRequiredKeyedService<IStep>(nameof(Step1ResolveNeighbours));
var step2RunChecks = serviceScope.ServiceProvider.GetRequiredKeyedService<IStep>(nameof(Step2RunChecks));

var asses = intentFileReader.ReadFiles(argumentsParser.FilePaths)
    .ExecuteStep(step1ResolveNeighbours)
    .ExecuteStep(step2RunChecks);

Log.Information("Processing complete.");
Log.Debug("ASs summary:\n{Summary}",asses.Summary());