using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RouterQuack.IntentFileReader;
using RouterQuack.IntentFileReader.Yaml;
using RouterQuack.Startup;

// Add DI
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<ArgumentParser>();
builder.Services.AddSingleton<IIntentFileReader, YamlReader>();
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
intentFileReader.ReadFiles(parser.FilePaths);
return 0;