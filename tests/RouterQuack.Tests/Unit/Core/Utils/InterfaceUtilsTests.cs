using RouterQuack.Core.Utils;

namespace RouterQuack.Tests.Unit.Core.Utils;

public class InterfaceUtilsTests
{
    private readonly InterfaceUtils _utils = new();

    [Test]
    [Arguments(null, BgpRelationship.None)]
    [Arguments("none", BgpRelationship.None)]
    [Arguments("client", BgpRelationship.Client)]
    [Arguments("CLIENT", BgpRelationship.Client)]
    [Arguments("peer", BgpRelationship.Peer)]
    [Arguments("PEER", BgpRelationship.Peer)]
    [Arguments("provider", BgpRelationship.Provider)]
    [Arguments("PROVIDER", BgpRelationship.Provider)]
    public async Task ParseBgp_ValidInput_ReturnsCorrectRelationship(string? input, BgpRelationship expected)
    {
        await Assert.That(_utils.ParseBgp(input))
            .IsEqualTo(expected);
    }

    [Test]
    [Arguments("invalid")]
    [Arguments("unknown")]
    public async Task ParseBgp_InvalidInput_ThrowsArgumentException(string input)
    {
        await Assert.That(() => _utils.ParseBgp(input))
            .Throws<ArgumentException>();
    }
}