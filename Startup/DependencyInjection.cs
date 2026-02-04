using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RouterQuack.IntentFileReader;
using RouterQuack.IntentFileReader.Yaml;
using RouterQuack.Steps;
using RouterQuack.Utils;

namespace RouterQuack.Startup;

public static class DependencyInjection
{
    public static IServiceScope CreateServiceScope(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        
        builder.Services.AddSingleton<IArgumentsParser, ArgumentsParser>();
        builder.Services.AddSingleton<INetworkUtils, NetworkUtils>();
        builder.Services.AddSingleton<IDisplayUtils, DisplayUtils>();
        
        builder.Services.AddSingleton<IIntentFileReader, YamlReader>();
        builder.Services.AddKeyedSingleton<IStep, Step2RunChecks>(nameof(Step2RunChecks));
        builder.Services.AddKeyedSingleton<IStep, Step1ResolveNeighbours>(nameof(Step1ResolveNeighbours));

        return builder.Build().Services.CreateScope();
    }
}