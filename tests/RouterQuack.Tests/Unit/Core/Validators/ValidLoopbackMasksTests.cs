using Microsoft.Extensions.Logging;
using NSubstitute;
using RouterQuack.Core.Validators;
using RouterQuack.Tests.Unit.TestHelpers;

namespace RouterQuack.Tests.Unit.Core.Validators;

public class ValidLoopbackMasksTests
{
    private readonly ILogger<ValidLoopbackMasks> _logger = Substitute.For<ILogger<ValidLoopbackMasks>>();

    [Test]
    public async Task Validate_ValidIpv4Loopback_NoErrors()
    {
        var asses = new List<As>
        {
            TestData.CreateAs(
                routers: [TestData.CreateRouter(loopbackAddressV4: IPAddress.Parse("10.0.0.1"))])
        };

        var context = ContextFactory.Create(asses: asses);
        var validator = new ValidLoopbackMasks(_logger, context);
        validator.Validate();

        await Assert.That(validator.ErrorsOccurred).IsFalse();
    }

    [Test]
    public async Task Validate_ValidIpv6Loopback_NoErrors()
    {
        var asses = new List<As>
        {
            TestData.CreateAs(
                routers: [TestData.CreateRouter(loopbackAddressV6: IPAddress.Parse("2001:db8::1"))])
        };

        var context = ContextFactory.Create(asses: asses);
        var validator = new ValidLoopbackMasks(_logger, context);
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
        var validator = new ValidLoopbackMasks(_logger, context);
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
        var validator = new ValidLoopbackMasks(_logger, context);
        validator.Validate();

        await Assert.That(validator.ErrorsOccurred).IsTrue();
    }

    [Test]
    public async Task Validate_NoLoopbackAddress_NoErrors()
    {
        var asses = new List<As>
        {
            TestData.CreateAs(routers: [TestData.CreateRouter(loopbackAddressV4: null, loopbackAddressV6: null)])
        };

        var context = ContextFactory.Create(asses: asses);
        var validator = new ValidLoopbackMasks(_logger, context);
        validator.Validate();

        await Assert.That(validator.ErrorsOccurred).IsFalse();
    }
}