using Microsoft.Extensions.Logging;
using NSubstitute;
using RouterQuack.Core.Validators;
using RouterQuack.Tests.Unit.TestHelpers;

namespace RouterQuack.Tests.Unit.Core.Validators;

public class WarningWhenAdditionalConfigTests
{
    private readonly ILogger<WarningWhenAdditionalConfig> _logger =
        Substitute.For<ILogger<WarningWhenAdditionalConfig>>();

    [Test]
    public async Task Validate_NoAdditionalConfigs_NoWarnings()
    {
        var asses = new List<As>
        {
            TestData.CreateAs(routers: [TestData.CreateRouter(interfaces: [TestData.CreateInterface()])])
        };

        var context = ContextFactory.Create(asses: asses, strict: true);
        var validator = new WarningWhenAdditionalConfig(_logger, context);
        validator.Validate();

        await Assert.That(validator.Context.ErrorsOccurred).IsFalse();
    }

    [Test]
    public async Task Validate_AdditionalConfigsInRouter_Warnings()
    {
        var router = TestData.CreateRouter(additionalConfig: "something", interfaces: [TestData.CreateInterface()]);
        var asses = new List<As>
        {
            TestData.CreateAs(routers: [router])
        };

        var context = ContextFactory.Create(asses: asses, strict: true);
        var validator = new WarningWhenAdditionalConfig(_logger, context);
        validator.Validate();

        await Assert.That(validator.Context.ErrorsOccurred).IsTrue();
    }

    [Test]
    public async Task Validate_AdditionalConfigsInInterface_Warnings()
    {
        var @interface = TestData.CreateInterface(additionalConfig: "something");
        var asses = new List<As>
        {
            TestData.CreateAs(routers: [TestData.CreateRouter(interfaces: [@interface])])
        };

        var context = ContextFactory.Create(asses: asses, strict: true);
        var validator = new WarningWhenAdditionalConfig(_logger, context);
        validator.Validate();

        await Assert.That(validator.Context.ErrorsOccurred).IsTrue();
    }
}