using System.Net;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RouterQuack.Core.Validators;
using RouterQuack.Tests.Unit.TestHelpers;

namespace RouterQuack.Tests.Unit.Core.Validators;

public class NoDuplicateLoopbackAddressTests
{
    private readonly ILogger<NoDuplicateLoopbackAddress>
        _logger = Substitute.For<ILogger<NoDuplicateLoopbackAddress>>();

    [Test]
    public async Task Validate_NoDuplicatesV4_NoErrors()
    {
        var routers = new List<Router>
        {
            TestData.CreateRouter(loopbackAddressV4: IPAddress.Parse("10.10.10.1")),
            TestData.CreateRouter(loopbackAddressV4: IPAddress.Parse("10.10.10.2"))
        };
        var asses = new List<As> { TestData.CreateAs(networksIpVersion: IpVersion.Ipv4, routers: routers) };

        var context = ContextFactory.Create(asses: asses);
        var validator = new NoDuplicateLoopbackAddress(_logger, context);
        validator.Validate();

        await Assert.That(validator.Context.ErrorsOccurred).IsFalse();
    }

    [Test]
    public async Task Validate_NoDuplicatesV6_NoErrors()
    {
        var routers = new List<Router>
        {
            TestData.CreateRouter(loopbackAddressV6: IPAddress.Parse("2001:1:1:1::1")),
            TestData.CreateRouter(loopbackAddressV6: IPAddress.Parse("2001:1:1:1::2"))
        };
        var asses = new List<As> { TestData.CreateAs(networksIpVersion: IpVersion.Ipv6, routers: routers) };

        var context = ContextFactory.Create(asses: asses);
        var validator = new NoDuplicateLoopbackAddress(_logger, context);
        validator.Validate();

        await Assert.That(validator.Context.ErrorsOccurred).IsFalse();
    }

    [Test]
    public async Task Validate_WithDuplicatesV4_SetsErrorsOccurred()
    {
        var routers = new List<Router>
        {
            TestData.CreateRouter(loopbackAddressV4: IPAddress.Parse("10.10.10.1")),
            TestData.CreateRouter(loopbackAddressV4: IPAddress.Parse("10.10.10.1"))
        };
        var asses = new List<As> { TestData.CreateAs(networksIpVersion: IpVersion.Ipv4, routers: routers) };

        var context = ContextFactory.Create(asses: asses);
        var validator = new NoDuplicateLoopbackAddress(_logger, context);
        validator.Validate();

        await Assert.That(validator.Context.ErrorsOccurred).IsTrue();
    }

    [Test]
    public async Task Validate_WithDuplicatesV6_SetsErrorsOccurred()
    {
        var routers = new List<Router>
        {
            TestData.CreateRouter(loopbackAddressV6: IPAddress.Parse("2001:1:1:1::1")),
            TestData.CreateRouter(loopbackAddressV6: IPAddress.Parse("2001:1:1:1::1"))
        };
        var asses = new List<As> { TestData.CreateAs(networksIpVersion: IpVersion.Ipv6, routers: routers) };

        var context = ContextFactory.Create(asses: asses);
        var validator = new NoDuplicateLoopbackAddress(_logger, context);
        validator.Validate();

        await Assert.That(validator.Context.ErrorsOccurred).IsTrue();
    }
}