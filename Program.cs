using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RouterQuack.Extensions;
using RouterQuack.IntentFileReader;
using RouterQuack.IntentFileReader.Yaml;
using RouterQuack.Startup;
using RouterQuack.Steps;
using RouterQuack.Utils;

// Add DI
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<ArgumentParser>();
builder.Services.AddSingleton<INetworkUtils, NetworkUtils>();
builder.Services.AddSingleton<IIntentFileReader, YamlReader>();
builder.Services.AddKeyedSingleton<IStep, Step2RunChecks>(nameof(Step2RunChecks));
builder.Services.AddKeyedSingleton<IStep, Step1ResolveNeighbours>(nameof(Step1ResolveNeighbours));
var host = builder.Build();
using var serviceScope = host.Services.CreateScope();

// Argument parser
var parser = serviceScope.ServiceProvider.GetRequiredService<ArgumentParser>();
var hasParsed = parser.Parse(args);
if (hasParsed.Any())
{
    Console.WriteLine("Parsing errors occured");
    return 1;
}

// Main
var intentFileReader = serviceScope.ServiceProvider.GetRequiredService<IIntentFileReader>();
var step1ResolveNeighbours = serviceScope.ServiceProvider.GetRequiredKeyedService<IStep>(nameof(Step1ResolveNeighbours));
var step2RunChecks = serviceScope.ServiceProvider.GetRequiredKeyedService<IStep>(nameof(Step2RunChecks));

var asses = intentFileReader.ReadFiles(parser.FilePaths)
    .ExecuteStep(step1ResolveNeighbours)
    .ExecuteStep(step2RunChecks);

asses.Display();
return 0;