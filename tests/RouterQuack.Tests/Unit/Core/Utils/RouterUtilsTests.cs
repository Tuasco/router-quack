using System.Net.Sockets;
using RouterQuack.Core.Utils;

namespace RouterQuack.Tests.Unit.Core.Utils;

public class RouterUtilsTests
{
    private readonly RouterUtils _utils = new();

    [Test]
    public async Task GetDefaultId_SameName_ReturnsSameId()
    {
        var id1 = _utils.GetDefaultId("Router1");
        var id2 = _utils.GetDefaultId("Router1");

        await Assert.That(id1).IsEqualTo(id2);
    }

    [Test]
    public async Task GetDefaultId_DifferentNames_ReturnsDifferentIds()
    {
        var id1 = _utils.GetDefaultId("Router1");
        var id2 = _utils.GetDefaultId("Router2");

        await Assert.That(id1).IsNotEqualTo(id2);
    }

    [Test]
    public async Task GetDefaultId_ReturnsValidIPv4()
    {
        var id = _utils.GetDefaultId("TestRouter");

        await Assert.That(id.AddressFamily).IsEqualTo(AddressFamily.InterNetwork);
        await Assert.That(id.GetAddressBytes()).Count().IsEqualTo(4);
    }

    [Test]
    [Arguments(null, RouterBrand.Cisco, null)]
    [Arguments(null, RouterBrand.Cisco, RouterBrand.Cisco)]
    [Arguments("cisco", RouterBrand.Cisco, null)]
    [Arguments("Cisco", RouterBrand.Cisco, null)]
    [Arguments("CISCO", RouterBrand.Cisco, null)]
    public async Task ParseBrand_ValidInput_ReturnsCorrectBrand(
        string? input,
        RouterBrand expected,
        RouterBrand? defaultBrand)
    {
        await Assert.That(() => _utils.ParseBrand(input, defaultBrand)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("invalid")]
    [Arguments("unknown")]
    [Arguments("juniper")]
    public async Task ParseBrand_InvalidInput_ThrowsArgumentException(string input)
    {
        await Assert.That(() => _utils.ParseBrand(input))
            .Throws<ArgumentException>();
    }
}