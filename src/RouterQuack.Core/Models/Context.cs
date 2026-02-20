using RouterQuack.Core.IntentFileParsers;
using RouterQuack.Core.Processors;
using RouterQuack.Core.Validators;

namespace RouterQuack.Core.Models;

public class Context
{
    public ICollection<As> Asses { get; } = [];

    /// <summary>
    /// Array of paths of intent files.
    /// </summary>
    public required string[] FilePaths { get; init; }

    public required string OutputDirectoryPath { get; init; }

    public required VerbosityLevel Verbosity { get; init; }

    public required bool DryRun { get; init; }

    public required bool Strict { get; init; }

    /// <summary>
    /// Call the execution of a config processor of the pipeline.
    /// </summary>
    /// <param name="step">The step to execute.</param>
    /// <returns>The modified collection of As objects. This is used to make a call chain.</returns>
    /// <exception cref="StepException">Step executed with errors.</exception>
    /// <exception cref="ArgumentException">Unsupported IStep derived interface.</exception>
    public Context ExecuteStep(IStep step)
    {
        LogBeginMessage(step);

        switch (step)
        {
            case IIntentFileParser intentFileParser:
                intentFileParser.ReadFiles(FilePaths);
                break;
            case IValidator validator:
                validator.Validate();
                break;
            case IProcessor processor:
                processor.Process();
                break;
            default:
                throw new ArgumentException("Unsupported step type", nameof(step));
        }

        if (step.ErrorsOccurred)
            throw new StepException();

        return this;
    }

    private static void LogBeginMessage(IStep step)
    {
        #pragma warning disable CA2254
        if (!string.IsNullOrWhiteSpace(step.BeginMessage))
            step.Logger.LogInformation(step.BeginMessage + "...");
        #pragma warning restore CA2254
    }
}

public enum VerbosityLevel
{
    Quiet, // -q
    Normal, // Default
    Detailed // -v
}