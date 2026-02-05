using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RouterQuack.Core.Steps;
using RouterQuack.Core.Utils;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using YamlReader = RouterQuack.IO.Yaml.YamlReader;

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
        var options = ArgumentsParser.CreateFromArgs(args);
        var minLevel = options.Verbosity switch
        {
            VerbosityLevel.Quiet => LogEventLevel.Warning,
            VerbosityLevel.Normal => LogEventLevel.Information,
            _ => LogEventLevel.Debug
        };

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(minLevel)
            .WriteTo.Console(theme: AnsiConsoleTheme.Literate)
            .CreateLogger();

        var builder = Host.CreateApplicationBuilder(args);

        // Register logger (Serilog)
        builder.Services.AddSerilog();

        // Register args parser
        builder.Services.AddSingleton<IArgumentsParser>(options);

        // Register utils
        builder.Services.AddSingleton<INetworkUtils, NetworkUtils>();
        builder.Services.AddSingleton<IRouterUtils, RouterUtils>();

        // Register steps
        builder.Services.AddSingleton<IIntentFileReader, YamlReader>();
        builder.Services.AddKeyedSingleton<IStep, Step1ResolveNeighbours>(nameof(Step1ResolveNeighbours));
        builder.Services.AddKeyedSingleton<IStep, Step2RunChecks>(nameof(Step2RunChecks));

        return builder.Build().Services.CreateScope();
    }
}