using System.Net;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RouterQuack.Core.Validators;
using RouterQuack.Tests.Unit.TestHelpers;

namespace RouterQuack.Tests.Unit.Core.Validators;

public class ValidLoopbackSpacesTests
{
    private readonly ILogger<ValidLoopbackSpaces> _logger = Substitute.For<ILogger<ValidLoopbackSpaces>>();

    [Test]
    [Arguments("10.10.10.0/24")]
    [Arguments("10.10.10.0/16")]
    [Arguments("10.10.10.10/32")]
    public async Task Validate_ValidIpv4LoopbackSpace_NoErrors(string address)
    {
        var asses = new List<As>
        {
            TestData.CreateAs(ipVersion: IpVersion.IPv4,
                loopbackSpaceV4: IPNetwork.Parse(address),
                routers: [TestData.CreateRouter()])
        };

        var context = ContextFactory.Create(asses: asses);
        var validator = new ValidLoopbackSpaces(_logger, context);
        validator.Validate();

        await Assert.That(validator.Context.ErrorsOccurred).IsFalse();
    }

    [Test]
    public async Task Process_MissingLoopbackSpaceV4_SetsErrorsOccurred()
    {
        var router = TestData.CreateRouter(loopbackAddressV4: null);
        var asses = new List<As>
        {
            TestData.CreateAs(loopbackSpaceV4: null, ipVersion: IpVersion.IPv4, routers: [router])
        };

        var context = ContextFactory.Create(asses: asses);
        var validator = new ValidLoopbackSpaces(_logger, context);
        validator.Validate();

        await Assert.That(validator.Context.ErrorsOccurred).IsTrue();
    }

    [Test]
    public async Task Process_MissingLoopbackSpaceV6_SetsErrorsOccurred()
    {
        var router = TestData.CreateRouter(loopbackAddressV6: null);
        var asses = new List<As>
        {
            TestData.CreateAs(loopbackSpaceV6: null, ipVersion: IpVersion.IPv6, routers: [router])
        };

        var context = ContextFactory.Create(asses: asses);
        var validator = new ValidLoopbackSpaces(_logger, context);
        validator.Validate();

        await Assert.That(validator.Context.ErrorsOccurred).IsTrue();
    }

    [Test]
    public async Task Process_LoopbackSpaceV4_PopulatedLoopbackAddresses_NoErrorsOccurred()
    {
        var loopbackAddress = IPAddress.Parse("10.10.10.1");
        var router = TestData.CreateRouter(loopbackAddressV4: loopbackAddress);
        var asses = new List<As>
        {
            TestData.CreateAs(loopbackSpaceV4: null, ipVersion: IpVersion.IPv4, routers: [router])
        };

        var context = ContextFactory.Create(asses: asses);
        var validator = new ValidLoopbackSpaces(_logger, context);
        validator.Validate();

        await Assert.That(router.LoopbackAddressV4).IsEqualTo(loopbackAddress);
        await Assert.That(validator.Context.ErrorsOccurred).IsFalse();
    }

    [Test]
    public async Task Process_LoopbackSpaceV6_PopulatedLoopbackAddresses_NoErrorsOccurred()
    {
        var loopbackAddress = IPAddress.Parse("2001:1:1:1::1");
        var router = TestData.CreateRouter(loopbackAddressV6: loopbackAddress);
        var asses = new List<As>
        {
            TestData.CreateAs(loopbackSpaceV6: null, ipVersion: IpVersion.IPv6, routers: [router])
        };

        var context = ContextFactory.Create(asses: asses);
        var validator = new ValidLoopbackSpaces(_logger, context);
        validator.Validate();

        await Assert.That(router.LoopbackAddressV6).IsEqualTo(loopbackAddress);
        await Assert.That(validator.Context.ErrorsOccurred).IsFalse();
    }

    [Test]
    public async Task Process_NoLoopbackSpaceV4_SetsErrorsOccurred()
    {
        var routers = new List<Router> { TestData.CreateRouter(loopbackAddressV4: null, external: false) };
        var asses = new List<As>
        {
            TestData.CreateAs(loopbackSpaceV4: null, ipVersion: IpVersion.IPv4, routers: routers)
        };

        var context = ContextFactory.Create(asses: asses);
        var validator = new ValidLoopbackSpaces(_logger, context);
        validator.Validate();

        await Assert.That(validator.Context.ErrorsOccurred).IsTrue();
    }

    [Test]
    public async Task Process_NoLoopbackSpaceV6_SetsErrorsOccurred()
    {
        var routers = new List<Router> { TestData.CreateRouter(loopbackAddressV6: null, external: false) };
        var asses = new List<As>
        {
            TestData.CreateAs(loopbackSpaceV6: null, ipVersion: IpVersion.IPv6, routers: routers)
        };

        var context = ContextFactory.Create(asses: asses);
        var validator = new ValidLoopbackSpaces(_logger, context);
        validator.Validate();

        await Assert.That(validator.Context.ErrorsOccurred).IsTrue();
    }
}