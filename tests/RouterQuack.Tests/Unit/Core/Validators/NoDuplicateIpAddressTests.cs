using Microsoft.Extensions.Logging;
using NSubstitute;
using RouterQuack.Core.Validators;
using RouterQuack.Tests.Unit.TestHelpers;

namespace RouterQuack.Tests.Unit.Core.Validators;

public class NoDuplicateIpAddressTests
{
    private readonly ILogger<NoDuplicateIpAddress> _logger = Substitute.For<ILogger<NoDuplicateIpAddress>>();

    [Test]
    [Arguments("192.168.1.1", "192.168.1.2", 24)]
    [Arguments("2001:db8::10", "2001:db8::20", 112)]
    public async Task Validate_NoDuplicates_NoErrors(string ip1, string ip2, int prefixLength)
    {
        var interfaces = new List<Interface>
        {
            TestData.CreateInterface(addresses: [TestData.CreateAddress(ip1, prefixLength)]),
            TestData.CreateInterface(addresses: [TestData.CreateAddress(ip2, prefixLength)])
        };
        var asses = new List<As> { TestData.CreateAs(routers: [TestData.CreateRouter(interfaces: interfaces)]) };

        var context = ContextFactory.Create(asses: asses);
        var validator = new NoDuplicateIpAddress(_logger, context);
        validator.Validate();

        await Assert.That(validator.ErrorsOccurred).IsFalse();
    }

    [Test]
    [Arguments("192.168.1.1", 24)]
    [Arguments("2001:db8::1", 127)]
    public async Task Validate_WithDuplicates_SetsErrorsOccurred(string ip, int prefixLength)
    {
        var interfaces = new List<Interface>
        {
            TestData.CreateInterface(
                addresses: [TestData.CreateAddress(ip, prefixLength)]),
            TestData.CreateInterface(
                addresses: [TestData.CreateAddress(ip, prefixLength)])
        };
        var asses = new List<As> { TestData.CreateAs(routers: [TestData.CreateRouter(interfaces: interfaces)]) };

        var context = ContextFactory.Create(asses: asses);
        var validator = new NoDuplicateIpAddress(_logger, context);
        validator.Validate();

        await Assert.That(validator.ErrorsOccurred).IsTrue();
    }

    [Test]
    public async Task Validate_EmptyAddresses_NoErrors()
    {
        var interfaces = new List<Interface>
        {
            TestData.CreateInterface(),
            TestData.CreateInterface()
        };
        var asses = new List<As> { TestData.CreateAs(routers: [TestData.CreateRouter(interfaces: interfaces)]) };

        var context = ContextFactory.Create(asses: asses);
        var validator = new NoDuplicateIpAddress(_logger, context);
        validator.Validate();

        await Assert.That(validator.ErrorsOccurred).IsFalse();
    }
}