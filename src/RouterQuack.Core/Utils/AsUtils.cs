using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using RouterQuack.Core.Models;

namespace RouterQuack.Core.Utils;

[SuppressMessage("Performance", "CA1822:Mark members as static")]
public sealed class AsUtils
{
    /// <param name="igp">An IGP (string format).</param>
    /// <returns>The corresponding IGP (Enum format).</returns>
    /// <exception cref="ArgumentException">Non <c>null</c> and unknown IGP.</exception>
    [Pure]
    public IgpType ParseIgp(string? igp)
    {
        if (igp == null)
            return IgpType.Ibgp;

        return Enum.TryParse<IgpType>(igp, true, out var igpType)
            ? igpType
            : throw new ArgumentException("Couldn't parse IGP");
    }
}