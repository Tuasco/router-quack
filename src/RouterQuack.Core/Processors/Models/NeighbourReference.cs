namespace RouterQuack.Core.Processors.Models;

/// <summary>
/// Parsed reference to a neighbour router, with an optional neighbour interface name.
/// </summary>
public readonly struct NeighbourReference
{
    public int AsNumber { get; }

    public string RouterName { get; }

    public string? InterfaceName { get; }

    /// <summary>
    /// Parse neighbour reference from path.
    /// </summary>
    /// <param name="neighbourPath">Path in `router[:interface]` or `as:router[:interface]` form.</param>
    /// <returns>Parsed <see cref="NeighbourReference"/>; invalid values produce an unresolvable reference.</returns>
    public NeighbourReference(string neighbourPath)
    {
        // The neighbour path should always look like ASN:RN:IN thanks to the parser
        var segments = neighbourPath.Split(':');

        (AsNumber, RouterName, InterfaceName) = segments.Length switch
        {
            3 when int.TryParse(segments[0], out var asNumber) => (asNumber, segments[1], segments[2]),
            _ => (0, string.Empty, null)
        };
    }

    /// <summary>
    /// Format a neighbour reference for logs and diagnostics.
    /// </summary>
    public override string ToString()
        => $"{AsNumber}:{RouterName}:{InterfaceName}";
}