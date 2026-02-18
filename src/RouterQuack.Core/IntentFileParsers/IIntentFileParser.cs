namespace RouterQuack.Core.IntentFileParsers;

/// <summary>
/// Describes an intent file reader and parser.
/// </summary>
public interface IIntentFileParser : IStep
{
    /// <summary>
    /// Read and parse an intent file.
    /// </summary>
    /// <param name="filePaths">Paths to the files to parse. Can safely include unrelated files like docs.</param>
    /// <returns>Parsed collection of populated As objects.</returns>
    /// <remarks>If <paramref name="filePaths"/> contains the path to a directory, it will be ignored.</remarks>
    public void ReadFiles(string[] filePaths);
}