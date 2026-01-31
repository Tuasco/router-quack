using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RouterQuack.IntentFileReader;

string[] filePaths = null!;

// Add DI
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<IIntentFileReader, YamlReader>();
var host = builder.Build();

// Argument parser
Option<IEnumerable<FileInfo>> fileOption = new("--file", "-f")
{
    Description = "Pass an intent file",
    DefaultValueFactory = _ => [new FileInfo("Examples/default.yaml")],
    Arity =  ArgumentArity.OneOrMore,
    AllowMultipleArgumentsPerToken = true
};

RootCommand rootCommand = new("Generate router configuration files from user-friendly intent files");
rootCommand.Options.Add(fileOption);
rootCommand.SetAction(parseResult => 
    filePaths = (parseResult.GetValue(fileOption) ?? []).Select(f => f.FullName).ToArray());

var parseResult = rootCommand.Parse(args);
parseResult.Invoke();
if (parseResult.Errors.Any())
{
    Console.WriteLine("Got parsing errors");
    return;
}

// Main
using IServiceScope serviceScope = host.Services.CreateScope();
{
    var provider = serviceScope.ServiceProvider;
    var intentFileReader = provider.GetRequiredService<IIntentFileReader>();
    intentFileReader.ReadFiles(filePaths);
}
