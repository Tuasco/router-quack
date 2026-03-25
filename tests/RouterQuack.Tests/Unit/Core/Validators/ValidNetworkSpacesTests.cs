using System.Net;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RouterQuack.Core.Validators;
using RouterQuack.Tests.Unit.TestHelpers;

namespace RouterQuack.Tests.Unit.Core.Validators;

public class ValidNetworkSpacesTests
{
    private readonly ILogger<ValidNetworkSpaces> _logger = Substitute.For<ILogger<ValidNetworkSpaces>>();

    [Test]
    [Arguments("10.0.0.0/8", "2001:db8::/32")]
    public async Task Validate_ValidSpaces_NoErrors(string v4Space, string v6Space)
    {
        var asses = new List<As>
        {
            TestData.CreateAs(
                networksSpaceV4: IPNetwork.Parse(v4Space),
                networksSpaceV6: IPNetwork.Parse(v6Space),
                ipVersion: IpVersion.IPv4 | IpVersion.IPv6)
        };

        var context = ContextFactory.Create(asses: asses);
        var validator = new ValidNetworkSpaces(_logger, context);
        validator.Validate();

        await Assert.That(validator.Context.ErrorsOccurred).IsFalse();
    }

    [Test]
    [Arguments("2001:db8::/32")]
    public async Task Validate_InvalidV4Space_SetsErrorsOccurred(string space)
    {
        var asses = new List<As>
        {
            TestData.CreateAs(
                networksSpaceV4: IPNetwork.Parse(space),
                ipVersion: IpVersion.IPv4)
        };

        var context = ContextFactory.Create(asses: asses);
        var validator = new ValidNetworkSpaces(_logger, context);
        validator.Validate();

        await Assert.That(validator.Context.ErrorsOccurred).IsTrue();
    }

    [Test]
    [Arguments("10.0.0.0/8")]
    public async Task Validate_InvalidV6Space_SetsErrorsOccurred(string space)
    {
        var asses = new List<As>
        {
            TestData.CreateAs(
                networksSpaceV6: IPNetwork.Parse(space),
                ipVersion: IpVersion.IPv6)
        };

        var context = ContextFactory.Create(asses: asses);
        var validator = new ValidNetworkSpaces(_logger, context);
        validator.Validate();

        await Assert.That(validator.Context.ErrorsOccurred).IsTrue();
    }

    [Test]
    public async Task Validate_FullyExternalAs_NoErrorsWhenMissingSpace()
    {
        var asses = new List<As>
        {
            TestData.CreateAs(
                routers: [TestData.CreateRouter(external: true)],
                networksSpaceV4: null,
                networksSpaceV6: null)
        };

        var context = ContextFactory.Create(asses: asses);
        var validator = new ValidNetworkSpaces(_logger, context);
        validator.Validate();

        await Assert.That(validator.Context.ErrorsOccurred).IsFalse();
    }
}