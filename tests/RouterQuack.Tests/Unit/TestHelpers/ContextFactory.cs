using AsContext = RouterQuack.Core.Models.Context;

namespace RouterQuack.Tests.Unit.TestHelpers;

internal static class ContextFactory
{
    internal static AsContext Create(
        ICollection<As>? asses = null,
        VerbosityLevel verbosity = VerbosityLevel.Normal,
        bool strict = false)
    {
        var context = new AsContext
        {
            FilePaths = [],
            OutputDirectoryPath = Path.GetTempPath(),
            Verbosity = verbosity,
            DebugGraph = false,
            DryRun = false,
            Deploy = false,
            Strict = strict
        };

        foreach (var @as in asses ?? [])
            context.Asses.Add(@as);

        return context;
    }
}