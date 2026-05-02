using Microsoft.Extensions.DependencyInjection;
using RouterQuack.Core.ConfigFileWriters;
using Serilog;

namespace RouterQuack.CLI.Pipeline;

public class WriteConfigs(IServiceProvider di) : IPipeline
{
    private readonly Context _context = di.GetRequiredService<Context>();

    public void Next()
    {
        Log.Information("Generating configuration files...");

        // Create output directory if not exists
        if (!Directory.Exists(_context.OutputDirectoryPath))
        {
            Log.Debug("Creating output directory.");
            Directory.CreateDirectory(_context.OutputDirectoryPath);
        }

        // Delete all files in there
        foreach (var file in Directory.EnumerateFiles(_context.OutputDirectoryPath))
            File.Delete(file);

        // Generate and write config files
        _context.ExecuteStep(di.GetRequiredKeyedService<IConfigFileWriter>(RouterBrand.Cisco));
    }
}