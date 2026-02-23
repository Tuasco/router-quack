using Microsoft.Extensions.Logging;
using NSubstitute;
using RouterQuack.Core.Validators;
using RouterQuack.Tests.Unit.TestHelpers;

namespace RouterQuack.Tests.Unit.Core.Validators;

public class NoExternalRouterWithoutAddressTests
{
    private readonly ILogger<NoExternalRouterWithoutAddress> _logger =
        Substitute.For<ILogger<NoExternalRouterWithoutAddress>>();

    [Test]
    [Arguments("192.168.1.0", "192.168.1.1", 24)]
    [Arguments("10.0.0.214", "10.0.0.215", 31)]
    [Arguments("2001:db8::", "2001:db8::1", 127)]
    public async Task Validate_ExternalWithAddresses_NoErrors(string ip1, string ip2, int prefixLength)
    {
        var neighbour = TestData.CreateInterface(
            addresses: [TestData.CreateAddress(ip1, prefixLength)]);
        var intf = TestData.CreateInterface(
            neighbour: neighbour,
            addresses: [TestData.CreateAddress(ip2, prefixLength)]);
        neighbour.Neighbour = intf;

        var asses = new List<As>
        {
            TestData.CreateAs(routers: [TestData.CreateRouter(external: true, interfaces: [intf])])
        };

        var context = ContextFactory.Create(asses: asses);
        var validator = new NoExternalRouterWithoutAddress(_logger, context);
        validator.Validate();

        await Assert.That(validator.ErrorsOccurred).IsFalse();
    }

    [Test]
    public async Task Validate_ExternalWithoutAddresses_SetsErrorsOccurred()
    {
        var neighbour = TestData.CreateInterface();
        var intf = TestData.CreateInterface(neighbour: neighbour);
        neighbour.Neighbour = intf;

        var asses = new List<As>
        {
            TestData.CreateAs(routers: [TestData.CreateRouter(external: true, interfaces: [intf])])
        };

        var context = ContextFactory.Create(asses: asses);
        var validator = new NoExternalRouterWithoutAddress(_logger, context);
        validator.Validate();

        await Assert.That(validator.ErrorsOccurred).IsTrue();
    }

    [Test]
    public async Task Validate_NonExternalWithoutAddresses_NoErrors()
    {
        var neighbour = TestData.CreateInterface();
        var intf = TestData.CreateInterface(neighbour: neighbour);
        neighbour.Neighbour = intf;

        var asses = new List<As>
        {
            TestData.CreateAs(routers: [TestData.CreateRouter(external: false, interfaces: [intf])])
        };

        var context = ContextFactory.Create(asses: asses);
        var validator = new NoExternalRouterWithoutAddress(_logger, context);
        validator.Validate();

        await Assert.That(validator.ErrorsOccurred).IsFalse();
    }
}