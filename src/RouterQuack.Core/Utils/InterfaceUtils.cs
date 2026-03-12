using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;

namespace RouterQuack.Core.Utils;

[SuppressMessage("Performance", "CA1822:Mark members as static")]
[SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Global")]
public sealed class InterfaceUtils
{
    /// <param name="bgp">A BGP relationship (string format).</param>
    /// <returns>The corresponding BGP relationship (Enum format).</returns>
    /// <exception cref="ArgumentException">Non <c>null</c> and unknown BGP relationship.</exception>
    [Pure]
    public BgpRelationship ParseBgp(string? bgp)
    {
        if (bgp == null)
            return BgpRelationship.None;

        return Enum.TryParse<BgpRelationship>(bgp, true, out var bgpRelationship)
            ? bgpRelationship
            : throw new ArgumentException("Couldn't parse BGP relationship");
    }
}