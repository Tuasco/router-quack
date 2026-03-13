using System.Net;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RouterQuack.Core.Validators;
using RouterQuack.Tests.Unit.TestHelpers;

namespace RouterQuack.Tests.Unit.Core.Validators;

public class ValidLoopbackAddressesTests
{
    private readonly ILogger<ValidLoopbackAddresses> _logger = Substitute.For<ILogger<ValidLoopbackAddresses>>();

    [Test]
    public async Task Validate_ValidIpv4Loopback_NoErrors()
    {
        var asses = new List<As>
        {
            TestData.CreateAs(
                routers: [TestData.CreateRouter(loopbackAddressV4: IPAddress.Parse("10.0.0.1"))])
        };

        var context = ContextFactory.Create(asses: asses);
        var validator = new ValidLoopbackAddresses(_logger, context);
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
        var validator = new ValidLoopbackAddresses(_logger, context);
        validator.Validate();

        await Assert.That(validator.ErrorsOccurred).IsFalse();
    }

    [Test]
    public async Task Validate_NoLoopbackAddress_NoErrors()
    {
        var asses = new List<As>
        {
            TestData.CreateAs(routers: [TestData.CreateRouter(loopbackAddressV4: null, loopbackAddressV6: null)])
        };

        var context = ContextFactory.Create(asses: asses);
        var validator = new ValidLoopbackAddresses(_logger, context);
        validator.Validate();

        await Assert.That(validator.ErrorsOccurred).IsFalse();
    }
}