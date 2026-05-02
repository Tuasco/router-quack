using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RouterQuack.Core.ConfigDeployers;
using RouterQuack.Core.ConfigFileWriters;
using RouterQuack.Core.IntentFileParsers;
using RouterQuack.Core.Processors;
using RouterQuack.Core.Utils;
using RouterQuack.Core.Validators;
using RouterQuack.IO.Cisco;
using RouterQuack.IO.Gns3;
using RouterQuack.IO.Gns3.Utils;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using YamlAsMapper = RouterQuack.IO.Yaml.Parser.YamlAsMapper;
using YamlInterfaceMapper = RouterQuack.IO.Yaml.Parser.YamlInterfaceMapper;
using YamlParser = RouterQuack.IO.Yaml.Parser.YamlParser;
using YamlRouterMapper = RouterQuack.IO.Yaml.Parser.YamlRouterMapper;

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
            .AddSingleton<RouterUtils>();

        builder.Services
            .AddSingleton<YamlInterfaceMapper>()
            .AddSingleton<YamlRouterMapper>()
            .AddSingleton<YamlAsMapper>();

        builder.Services
            .AddSingleton<Gns3ApiClient>();

        // Register steps
        builder.Services.AddSingleton<IIntentFileParser, YamlParser>();

        builder.Services
            .AddKeyedSingleton<IValidator, NoDuplicateIpAddress>(nameof(NoDuplicateIpAddress))
            .AddKeyedSingleton<IValidator, NoDuplicateLoopbackAddress>(nameof(NoDuplicateLoopbackAddress))
            .AddKeyedSingleton<IValidator, NoDuplicateRouterName>(nameof(NoDuplicateRouterName))
            .AddKeyedSingleton<IValidator, NoExternalRouterWithoutAddress>(nameof(NoExternalRouterWithoutAddress))
            .AddKeyedSingleton<IValidator, ValidBgpRelationships>(nameof(ValidBgpRelationships))
            .AddKeyedSingleton<IValidator, ValidLoopbackAddresses>(nameof(ValidLoopbackAddresses))
            .AddKeyedSingleton<IValidator, ValidLoopbackSpaces>(nameof(ValidLoopbackSpaces))
            .AddKeyedSingleton<IValidator, ValidNetworkSpaces>(nameof(ValidNetworkSpaces))
            .AddKeyedSingleton<IValidator, ValidVrfReferences>(nameof(ValidVrfReferences))
            .AddKeyedSingleton<IValidator, WarningWhenAdditionalConfig>(nameof(WarningWhenAdditionalConfig))
            .AddKeyedSingleton<IValidator, Ipv4SetWhenLdpIs>(nameof(Ipv4SetWhenLdpIs));


        builder.Services
            .AddKeyedSingleton<IProcessor, ResolveNeighbours>(nameof(ResolveNeighbours))
            .AddKeyedSingleton<IProcessor, GenerateLinkAddresses>(nameof(GenerateLinkAddresses))
            .AddKeyedSingleton<IProcessor, GenerateLoopbackAddresses>(nameof(GenerateLoopbackAddresses))
            .AddKeyedSingleton<IProcessor, PopulateRouterIds>(nameof(PopulateRouterIds))
            .AddKeyedSingleton<IProcessor, PopulateVrfRdRt>(nameof(PopulateVrfRdRt))
            .AddKeyedSingleton<IProcessor, ToggleIbgp>(nameof(ToggleIbgp));

        builder.Services
            .AddKeyedSingleton<IConfigFileWriter, CiscoWriter>(RouterBrand.Cisco);

        builder.Services
            .AddSingleton<IConfigDeployer, Gns3Deployer>();

        return builder.Build().Services.CreateScope();
    }
}