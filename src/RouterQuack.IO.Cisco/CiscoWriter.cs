using System.Text;
using Microsoft.Extensions.Logging;
using RouterQuack.Core.ConfigFileWriters;
using RouterQuack.Core.Models;
using RouterQuack.IO.Cisco.Utils;

namespace RouterQuack.IO.Cisco;

public class CiscoWriter(ILogger<CiscoWriter> logger, Context context) : IConfigFileWriter
{
    public bool ErrorsOccurred { get; set; }
    public string BeginMessage => "Writing Cisco config files";
    public ILogger Logger { get; } = logger;
    public Context Context { get; } = context;

    public RouterBrand Brand { get; init; } = RouterBrand.Cisco;

    public void WriteFiles(string outputDirectory)
    {
        if (!Directory.Exists(outputDirectory))
            throw new DirectoryNotFoundException(outputDirectory);

        foreach (var @as in Context.Asses)
        {
            var path = Path.Join(outputDirectory, @as.Number.ToString());
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            foreach (var router in @as.Routers)
            {
                var builder = new StringBuilder();
                InitialConfig.ApplyInitialConfig(builder, router.Name);
                OspfConfig.ApplyOspfConfig(builder, router.Id);
                BgpConfig.ApplyBgpConfig(builder, router);
                InterfacesConfig.ApplyInterfacesConfig(builder, router);
                UnusedServicesConfig.ApplyUnusedServicesConfig(builder);
                LoggingConfig.ApplyLoggingConfig(builder);
                builder.Append("end");
                Console.WriteLine(builder.ToString());
            }
        }
    }
}