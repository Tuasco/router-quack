namespace RouterQuack.Core.Extensions;

/// <summary>
/// Extension methods for <see cref="Context"/>.
/// </summary>
public static class ContextExtensions
{
    /// <summary>
    /// Mark the current execution context as failed.
    /// </summary>
    /// <param name="context">The execution context to update.</param>
    public static void ApplyError(this Context context)
    {
        context.ErrorsOccurred = true;
    }
    /// <summary>
    /// Mark the current execution context as warning.
    /// </summary>
    /// <param name="context">The execution context to update.</param>
    public static void ApplyWarning(this Context context)
    {
        if (context.Strict)
            context.ErrorsOccurred = true;
    }
}
