using System.Text;
using Microsoft.Extensions.Logging;
using RouterQuack.Core.ConfigFileWriters;
using RouterQuack.Core.Models;
using RouterQuack.IO.Cisco.Utils;
using BgpConfig = RouterQuack.IO.Cisco.Utils.BgpConfig;

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
            if (@as.FullyExternal)
                continue;

            var path = Path.Join(outputDirectory, @as.Number.ToString());
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            foreach (var router in @as.Routers)
            {
                if (router.External)
                    continue;

                var builder = new StringBuilder();
                InitialConfig.ApplyInitialConfig(builder, router.Name);
                OspfConfig.ApplyOspfConfig(builder, router.Id!, router.ParentAs.IpVersions);
                InterfacesConfig.ApplyInterfacesConfig(builder, router);
                BgpConfig.ApplyBgpConfig(builder, router);
                UnusedServicesConfig.ApplyUnusedServicesConfig(builder);
                LoggingConfig.ApplyLoggingConfig(builder);
                AdditionalRouterConfig.ApplyAdditionalRouterConfig(builder, router);
                builder.Append("end");

                using var file = new FileStream(Path.Join(path, $"{router.Name}.cfg"), FileMode.Create);
                file.Write(Encoding.UTF8.GetBytes(builder.ToString()));
            }
        }
    }
}