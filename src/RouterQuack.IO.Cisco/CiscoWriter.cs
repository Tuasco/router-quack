using Microsoft.Extensions.Logging;
using RouterQuack.Core.ConfigFileWriters;
using RouterQuack.Core.Models;

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
    }
}