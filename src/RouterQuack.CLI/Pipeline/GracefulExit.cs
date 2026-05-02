using RouterQuack.Core.Extensions;
using Serilog;

namespace RouterQuack.CLI.Pipeline;

public static class GracefulExit
{
    public static void OnStepException()
    {
        Log.Fatal("Exited with errors. Nothing changed.");
        Environment.ExitCode = 1;
    }

    public static void OnUnhandledException(Exception e, Context context)
    {
        Log.Fatal("Unhandled exception occured.");
        Log.Debug("Exception:\n{Exception}", e);
        Log.Debug("ASs summary:\n{Summary}", context.Asses.Summary());
        Environment.ExitCode = 2;
    }

    public static void OnDebugEnabled(Context context)
    {
        if (!context.DebugGraph)
            return;

        Console.WriteLine("ASs summary: {0}", context.Asses.Summary());
    }
}