using RouterQuack.Core.Utils;

namespace RouterQuack.Tests.Unit.Core.Utils;

public class AsUtilsTests
{
    private readonly AsUtils _utils = new();

    [Test]
    [Arguments(null, IgpType.Ibgp)]
    [Arguments("ibgp", IgpType.Ibgp)]
    [Arguments("IBGP", IgpType.Ibgp)]
    [Arguments("Ibgp", IgpType.Ibgp)]
    public async Task ParseIgp_ValidInput_ReturnsCorrectIgp(string? input, IgpType expected)
    {
        await Assert.That(() => _utils.ParseIgp(input))
            .IsEqualTo(expected);
    }

    [Test]
    [Arguments("invalid")]
    [Arguments("unknown")]
    [Arguments("ospf")]
    [Arguments("rip")]
    public async Task ParseIgp_InvalidInput_ThrowsArgumentException(string input)
    {
        await Assert.That(() => _utils.ParseIgp(input))
            .Throws<ArgumentException>();
    }
}