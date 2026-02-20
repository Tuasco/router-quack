using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RouterQuack.Core.IntentFileParsers;
using RouterQuack.Core.Processors;
using RouterQuack.Core.Utils;
using RouterQuack.Core.Validators;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using YamlParser = RouterQuack.IO.Yaml.Parser.YamlParser;

namespace RouterQuack.CLI.Startup;

public static class DependencyInjection
{
    /// <summary>
    /// Register services and build host.
    /// </summary>
    /// <param name="args">CLI arguments.</param>
    /// <returns>A service scope to inject dependencies from.</returns>
    public static IServiceScope CreateServiceScope(string[] args)
    {
        // Parse arguments
        var context = ArgumentsParser.CreateFromArgs(args);
        var minLevel = context.Verbosity switch
        {
            VerbosityLevel.Quiet => LogEventLevel.Warning,
            VerbosityLevel.Normal => LogEventLevel.Information,
            _ => LogEventLevel.Debug
        };

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(minLevel)
            .WriteTo.Console(
                theme: AnsiConsoleTheme.Sixteen,
                outputTemplate: "[{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        var builder = Host.CreateApplicationBuilder(args);

        // Register context
        builder.Services.AddSingleton(context);

        // Register logger (Serilog)
        builder.Services.AddSerilog();

        // Register utils
        builder.Services
            .AddSingleton<NetworkUtils>()
            .AddSingleton<AsUtils>()
            .AddSingleton<RouterUtils>()
            .AddSingleton<InterfaceUtils>();

        // Register steps
        builder.Services.AddSingleton<IIntentFileParser, YamlParser>();

        builder.Services
            .AddKeyedSingleton<IValidator, NoDuplicateIpAddress>(nameof(NoDuplicateIpAddress))
            .AddKeyedSingleton<IValidator, NoDuplicateRouterNames>(nameof(NoDuplicateRouterNames))
            .AddKeyedSingleton<IValidator, NoExternalRouterWithoutAddress>(nameof(NoExternalRouterWithoutAddress))
            .AddKeyedSingleton<IValidator, ValidBgpRelationships>(nameof(ValidBgpRelationships))
            .AddKeyedSingleton<IValidator, ValidLoopbackAddresses>(nameof(ValidLoopbackAddresses))
            .AddKeyedSingleton<IValidator, ValidNetworkSpaces>(nameof(ValidNetworkSpaces));

        builder.Services
            .AddKeyedSingleton<IProcessor, ResolveNeighbours>(nameof(ResolveNeighbours))
            .AddKeyedSingleton<IProcessor, GenerateLinkAddresses>(nameof(GenerateLinkAddresses))
            .AddKeyedSingleton<IProcessor, GenerateLoopbackAddresses>(nameof(GenerateLoopbackAddresses));

        return builder.Build().Services.CreateScope();
    }
}