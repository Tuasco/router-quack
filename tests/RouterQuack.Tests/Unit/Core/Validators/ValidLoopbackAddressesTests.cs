using Microsoft.Extensions.Logging;
using NSubstitute;
using RouterQuack.Core.Validators;
using RouterQuack.Tests.Unit.TestHelpers;

namespace RouterQuack.Tests.Unit.Core.Validators;

public class ValidLoopbackAddressesTests
{
    private readonly ILogger<ValidLoopbackAddresses> _logger = Substitute.For<ILogger<ValidLoopbackAddresses>>();

    [Test]
    [Arguments("10.0.0.1", 32)]
    public async Task Validate_ValidIpv4Loopback_NoErrors(string address, int prefixLength)
    {
        var asses = new List<As>
        {
            TestData.CreateAs(
                routers: [TestData.CreateRouter(loopbackAddress: TestData.CreateAddress(address, prefixLength))])
        };

        var context = ContextFactory.Create(asses: asses);
        var validator = new ValidLoopbackAddresses(_logger, context);
        validator.Validate();

        await Assert.That(validator.ErrorsOccurred).IsFalse();
    }

    [Test]
    [Arguments("2001:db8::1", 128)]
    public async Task Validate_ValidIpv6Loopback_NoErrors(string address, int prefixLength)
    {
        var asses = new List<As>
        {
            TestData.CreateAs(
                routers: [TestData.CreateRouter(loopbackAddress: TestData.CreateAddress(address, prefixLength))])
        };

        var context = ContextFactory.Create(asses: asses);
        var validator = new ValidLoopbackAddresses(_logger, context);
        validator.Validate();

        await Assert.That(validator.ErrorsOccurred).IsFalse();
    }

    [Test]
    [Arguments("10.0.0.1", 24)]
    public async Task Validate_InvalidIpv4Prefix_SetsErrorsOccurred(string address, int prefixLength)
    {
        var asses = new List<As>
        {
            TestData.CreateAs(
                routers: [TestData.CreateRouter(loopbackAddress: TestData.CreateAddress(address, prefixLength))])
        };

        var context = ContextFactory.Create(asses: asses);
        var validator = new ValidLoopbackAddresses(_logger, context);
        validator.Validate();

        await Assert.That(validator.ErrorsOccurred).IsTrue();
    }

    [Test]
    [Arguments("2001:db8::1", 64)]
    public async Task Validate_InvalidIpv6Prefix_SetsErrorsOccurred(string address, int prefixLength)
    {
        var asses = new List<As>
        {
            TestData.CreateAs(
                routers: [TestData.CreateRouter(loopbackAddress: TestData.CreateAddress(address, prefixLength))])
        };

        var context = ContextFactory.Create(asses: asses);
        var validator = new ValidLoopbackAddresses(_logger, context);
        validator.Validate();

        await Assert.That(validator.ErrorsOccurred).IsTrue();
    }

    [Test]
    public async Task Validate_NoLoopbackAddress_NoErrors()
    {
        var asses = new List<As>
        {
            TestData.CreateAs(routers: [TestData.CreateRouter(loopbackAddress: null)])
        };

        var context = ContextFactory.Create(asses: asses);
        var validator = new ValidLoopbackAddresses(_logger, context);
        validator.Validate();

        await Assert.That(validator.ErrorsOccurred).IsFalse();
    }
}